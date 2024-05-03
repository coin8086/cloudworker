using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using CloudWorker.Client.SDK.Bicep;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CloudWorker.Client.SDK;

public interface ICluster
{
    string Id { get; }

    Task CreateOrUpdateAsync();

    Task GetAsync();

    Task DestroyAsync();

    Task<QueueProperties> GetQueueProperties();
}

public class Cluster : ICluster
{
    private readonly TokenCredential _credential;

    public readonly ClusterId _clusterId;

    private readonly ClusterConfig? _clusterConfig;

    public string Id => _clusterId.ToString();

    private ILogger? _logger;

    //To create a new cluster
    public Cluster(TokenCredential credential, ClusterConfig clusterConfig, ILogger<Cluster>? logger = null) 
        : this(credential, clusterConfig, null, logger) {}

    //To get an existing cluster
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
            var parameters = CreateStarterParameters(_clusterConfig, _clusterId.ResourceId);
            var deploymentName = $"{this.GetType().FullName}:{_clusterId.ResourceId}";
            var deploymentData = new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(template),
                Parameters = BinaryData.FromObjectAsJson(parameters)
            });

            var client = new ArmClient(_credential, _clusterConfig.SubScriptionId);
            var sub = client.GetDefaultSubscription();
            var deployments = sub.GetArmDeployments();
            var result = await deployments.CreateOrUpdateAsync(Azure.WaitUntil.Completed, deploymentName, deploymentData);
            var deployment = result.Value;

            //...
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error when creating cluster.");
            throw;
        }
    }

    public Task GetAsync()
    {
        throw new NotImplementedException();
    }

    public Task DestroyAsync()
    {
        throw new NotImplementedException();
    }

    public Task<QueueProperties> GetQueueProperties()
    {
        throw new NotImplementedException();
    }


    private static StarterParameters CreateStarterParameters(ClusterConfig clusterConfig, Guid resourceId)
    {
        throw new NotImplementedException();
    }
}
