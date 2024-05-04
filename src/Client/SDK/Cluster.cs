using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using CloudWorker.Client.SDK.Bicep;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CloudWorker.Client.SDK;

public interface ICluster
{
    string Id { get; }

    Task CreateOrUpdateAsync();

    Task ValidateAsync();

    Task DestroyAsync();

    Task<ClusterProperties> GetPropertiesAsync();
}

public class Cluster : ICluster
{
    private readonly TokenCredential _credential;

    private readonly ClusterId _clusterId;

    private readonly ClusterConfig? _clusterConfig;

    private ILogger? _logger;

    public string Id => _clusterId.ToString();

    private string DeploymentName => $"{this.GetType().FullName}:{_clusterId.ResourceId}";

    private string MessagingRgName => $"{_clusterId.ResourceId}-messaging";

    private string ComputingRgName => $"{_clusterId.ResourceId}-computing";

    //NOTE: The following tags are defined in starter.bicep. Keey them up to date!
    private const string QueueTypeTag = "QueueType";
    private const string RequestQueueNameTag = "RequestQueueName";
    private const string ResponseQueueNameTag = "ResponseQueueName";

    //To create a new cluster
    public Cluster(TokenCredential credential, ClusterConfig clusterConfig, ILogger<Cluster>? logger = null) 
        : this(credential, clusterConfig, null, logger) {}

    //To use an existing cluster
    public Cluster(TokenCredential credential, string clusterId, ILogger<Cluster>? logger = null)
    {
        _clusterId = ClusterId.FromString(clusterId);
        _credential = credential;
        _logger = logger;
    }

    //To create or update a cluster
    public Cluster(TokenCredential credential, ClusterConfig clusterConfig, string? clusterId = null, ILogger<Cluster>? logger = null)
    {
        clusterConfig.Validate();
        if (clusterId != null)
        {
            var clsId = ClusterId.FromString(clusterId);
            var subId = Guid.Parse(clusterConfig.SubScriptionId);
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
    }

    public async Task CreateOrUpdateAsync()
    {
        if (_clusterConfig == null)
        {
            throw new InvalidOperationException("Cannot create a cluster without ClusterConfig!");
        }

        try
        {
            var baseDir = Path.GetDirectoryName(typeof(Cluster).Assembly.Location);
            var templateFile = Path.Join(baseDir, "Bicep", "starter.bicep");
            var template = File.ReadAllText(templateFile);
            var parameters = NewTemplateParameters();
            var deploymentData = new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(template),
                Parameters = BinaryData.FromObjectAsJson(parameters)
            });

            var client = new ArmClient(_credential, _clusterConfig.SubScriptionId);
            var sub = client.GetDefaultSubscription();
            var deployments = sub.GetArmDeployments();
            var result = await deployments.CreateOrUpdateAsync(Azure.WaitUntil.Completed, DeploymentName, deploymentData);
            var deployment = result.Value;

            //...
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error when creating cluster.");
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
            Service = _clusterConfig.Service.ToString().ToLower(),
            EnvironmentVariables = _clusterConfig.EnvironmentVariables,
            FileShareMounts = _clusterConfig.FileShareMounts,
            MessagingRgName = MessagingRgName,
            ComputingRgName = ComputingRgName,
        };
        parameters.Validate();
        return parameters;
    }

    public Task ValidateAsync()
    {
        throw new NotImplementedException();
    }

    public Task DestroyAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<ClusterProperties> GetPropertiesAsync()
    {
        try
        {
            var client = new ArmClient(_credential, _clusterId.SubscriptionId.ToString());
            var sub = client.GetDefaultSubscription();

            var qTask = GetQueuePropertiesAsync(sub);
            var sTask = GetServicePropertiesAsync(sub);

            await Task.WhenAll(qTask, sTask);

            return new ClusterProperties()
            {
                QueueProperties = qTask.Result,
                ServiceProperties = sTask.Result
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error when getting queue properties.");
            throw;
        }
    }

    private async Task<QueueProperties> GetQueuePropertiesAsync(SubscriptionResource subscription)
    {
        var result = await subscription.GetResourceGroupAsync(MessagingRgName);
        var rg = result.Value;
        var queueProperties = new QueueProperties();

        //Get some properties by tags on messaging rg
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
        //TODO: Get queue connection string...

        throw new NotImplementedException();
    }

    private Task<ServiceProperties> GetServicePropertiesAsync(SubscriptionResource subscription)
    {
        throw new NotImplementedException();
    }
}
