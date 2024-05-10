using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.ServiceBus.Models;
using CloudWorker.Client.SDK.Bicep;
using CloudWorker.MessageQueue;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker.Client.SDK;

public interface ICluster
{
    string Id { get; }

    Task CreateOrUpdateAsync(CancellationToken token = default);

    Task ValidateAsync(CancellationToken token = default);

    Task DestroyAsync(CancellationToken token = default);

    //TODO: Or: Task<IDictionary<string, string>> GetPropertiesAsync() ?
    Task<ClusterProperties> GetPropertiesAsync(CancellationToken token = default);
}

public class Cluster : ICluster
{
    private readonly TokenCredential _credential;

    private readonly ClusterId _clusterId;

    private readonly ClusterConfig? _clusterConfig;

    private ILogger? _logger;

    public string Id => _clusterId.ToString();

    //NOTE: Some Azure RPs, like Service Bus, do not allow a resource name starting with a digit number.
    //So here a name prefix "cw-" (short for CloudWorker) is used.
    private string DeploymentName => $"cw-{_clusterId.ResourceId}-deployment";

    private string MessagingRgName => $"cw-{_clusterId.ResourceId}-messaging";

    private string ComputingRgName => $"cw-{_clusterId.ResourceId}-computing";

    private string ServiceBusName => $"cw-{_clusterId.ResourceId}-servicebus";

    private string AppInsightsName => $"cw-{_clusterId.ResourceId}-appinsights";

    //NOTE: The following tags are defined in starter.bicep. Keey them up to date!
    private const string QueueTypeTag = "QueueType";
    private const string RequestQueueNameTag = "RequestQueueName";
    private const string ResponseQueueNameTag = "ResponseQueueName";
    private const string ServiceTag = "Service";

    //To create a new cluster
    public Cluster(TokenCredential credential, ClusterConfig clusterConfig, ILogger<Cluster>? logger = null) 
        : this(credential, clusterConfig, null, logger) {}

    //To use an existing cluster
    public Cluster(TokenCredential credential, string clusterId, ILogger<Cluster>? logger = null)
    {
        _clusterId = ClusterId.FromString(clusterId);
        _credential = credential;
        _logger = logger;
        _logger?.LogInformation("Cluster ID: {id}", _clusterId);
    }

    //To create or update a cluster
    public Cluster(TokenCredential credential, ClusterConfig clusterConfig, string? clusterId = null, ILogger<Cluster>? logger = null)
    {
        clusterConfig.Validate();
        if (clusterId != null)
        {
            var clsId = ClusterId.FromString(clusterId);
            var subId = clusterConfig.SubScriptionId;
            if (subId != clsId.SubscriptionId)
            {
                throw new ArgumentException("SubScription IDs are inconsistent!");
            }
            //Use specified id to create or update a cluster
            _clusterId = clsId;
        }
        else
        {
            //Create a new id to create a new cluster
            _clusterId = new ClusterId(clusterConfig.SubScriptionId, Guid.NewGuid());
        }
        _clusterConfig = clusterConfig;
        _credential = credential;
        _logger = logger;
        _logger?.LogInformation("Cluster ID: {id}", _clusterId);
    }

    public async Task CreateOrUpdateAsync(CancellationToken token = default)
    {
        if (_clusterConfig == null)
        {
            var msg = "Cannot create a cluster without ClusterConfig!";
            _logger?.LogError(msg);
            throw new InvalidOperationException(msg);
        }

        _logger?.LogInformation("Create or update cluster {id}", _clusterId);
        try
        {
            var baseDir = Path.GetDirectoryName(typeof(Cluster).Assembly.Location);
            var templateFile = Path.Join(baseDir, "ArmTemplates", "starter.json");
            _logger?.LogInformation("Use template {file}", templateFile);

            var template = File.ReadAllText(templateFile);
            var parameters = NewTemplateParameters();
            var jsonOptions = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var parametersInJson = JsonSerializer.Serialize(parameters, jsonOptions);
            _logger?.LogDebug("Template parameters in JSON:\n{content}", parametersInJson);

            var deploymentProperties = new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(template),
                Parameters = BinaryData.FromString(parametersInJson)
            };
            var deploymentData = new ArmDeploymentContent(deploymentProperties) { Location = _clusterConfig.Location };

            var client = new ArmClient(_credential, _clusterConfig.SubScriptionId.ToString());
            var sub = client.GetDefaultSubscription();
            var deployments = sub.GetArmDeployments();

            _logger?.LogInformation("Create or update deployment {name}", DeploymentName);
            var result = await deployments.CreateOrUpdateAsync(Azure.WaitUntil.Completed, DeploymentName, deploymentData, token);
            var deployment = result.Value;

            _logger?.LogInformation("Finish deployment {name}", DeploymentName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error when creating or updating cluster {id}.", _clusterId);
            throw;
        }
    }

    private StarterParameters NewTemplateParameters()
    {
        Debug.Assert(_clusterConfig != null);

        if (_clusterConfig.Service == ServiceType.Custom)
        {
            throw new NotImplementedException();
        }

        var parameters = new StarterParameters()
        {
            Location = ArmParamValue<string>.Create(_clusterConfig.Location),
            Service = ArmParamValue<string>.Create(_clusterConfig.Service.ToString().ToLower()),
            EnvironmentVariables = ArmParamValue<IEnumerable<SecureEnvironmentVariable>>.Create(_clusterConfig.EnvironmentVariables),
            FileShares = ArmParamValue<IEnumerable<FileShareMount>>.Create(_clusterConfig.FileShares),
            MessagingRgName = ArmParamValue<string>.Create(MessagingRgName),
            ComputingRgName = ArmParamValue<string>.Create(ComputingRgName),
            ServiceBusName = ArmParamValue<string>.Create(ServiceBusName),
            AppInsightsName = ArmParamValue<string>.Create(AppInsightsName),
        };
        return parameters;
    }

    //TODO: Remove the method or provide real implementation
    public Task ValidateAsync(CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public async Task DestroyAsync(CancellationToken token = default)
    {
        _logger?.LogInformation("Destroy cluster {id}", _clusterId);
        try
        {
            var client = new ArmClient(_credential, _clusterId.SubscriptionId.ToString());
            var sub = client.GetDefaultSubscription();
            var qTask = DeleteResourceGroupAsync(sub, MessagingRgName);
            var cTask = DeleteResourceGroupAsync(sub, ComputingRgName);
            await Task.WhenAll(qTask, cTask);
            _logger?.LogInformation("Cluster {id} is destroyed.", _clusterId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error when destroying cluster {id}.", _clusterId);
            throw;
        }
    }

    private async Task DeleteResourceGroupAsync(SubscriptionResource subscription, string rgName, CancellationToken token = default)
    {
        _logger?.LogInformation("Delete resource group {name} of subscription {id}", rgName, subscription.Id);
        try
        {
            ResourceGroupResource rg = await subscription.GetResourceGroupAsync(rgName, token);
            await rg.DeleteAsync(Azure.WaitUntil.Completed, cancellationToken: token);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger?.LogWarning("Resource group {name} is not found in subscription {id}.", rgName, subscription.Id);
        }
    }

    //TODO: Get property for monitoring/AppInsights URL
    public async Task<ClusterProperties> GetPropertiesAsync(CancellationToken token = default)
    {
        try
        {
            var client = new ArmClient(_credential, _clusterId.SubscriptionId.ToString());
            var sub = client.GetDefaultSubscription();

            var qTask = GetQueuePropertiesAsync(sub, token);
            var sTask = GetServicePropertiesAsync(sub, token);

            await Task.WhenAll(qTask, sTask);

            return new ClusterProperties()
            {
                QueueProperties = qTask.Result,
                ServiceProperties = sTask.Result
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error when getting properties of cluster {id}.", _clusterId);
            throw;
        }
    }

    private async Task<QueueProperties> GetQueuePropertiesAsync(SubscriptionResource subscription, CancellationToken token = default)
    {
        ResourceGroupResource rg = await subscription.GetResourceGroupAsync(MessagingRgName, token);

        var queueProperties = new QueueProperties();
        foreach (var tag in rg.Data.Tags)
        {
            switch (tag.Key)
            {
                case QueueTypeTag:
                    queueProperties.QueueType = tag.Value;
                    break;
                case RequestQueueNameTag:
                    queueProperties.RequestQueueName = tag.Value;
                    break;
                case ResponseQueueNameTag:
                    queueProperties.ResponseQueueName = tag.Value;
                    break;
            }
        }

        //TODO: Support getting connection string for Storage Queue
        if (!ServiceBusQueue.QueueType.Equals(queueProperties.QueueType, StringComparison.OrdinalIgnoreCase))
        {
            return queueProperties;
        }

        ServiceBusNamespaceResource sb = await rg.GetServiceBusNamespaceAsync(ServiceBusName, token);
        ServiceBusNamespaceAuthorizationRuleResource rule = await sb.GetServiceBusNamespaceAuthorizationRuleAsync("RootManageSharedAccessKey", token);
        ServiceBusAccessKeys keys = await rule.GetKeysAsync(token);
        queueProperties.ConnectionString = keys.PrimaryConnectionString;

        return queueProperties;
    }

    private async Task<ServiceProperties> GetServicePropertiesAsync(SubscriptionResource subscription, CancellationToken token = default)
    {
        ResourceGroupResource rg = await subscription.GetResourceGroupAsync(ComputingRgName, token);

        var serviceProperties = new ServiceProperties();
        foreach (var tag in rg.Data.Tags)
        {
            switch (tag.Key)
            {
                case ServiceTag:
                    serviceProperties.Service = tag.Value;
                    break;
            }
        }
        return serviceProperties;
    }
}
