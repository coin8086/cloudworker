using Azure.Core;
using CloudWorker.MessageQueue;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker.Client.SDK;

public interface ISession
{
    string Id { get; }

    IClusterProperties ClusterProperties { get; }

    IMessageQueue CreateSender();

    IMessageQueue CreateReceiver();
}

public class SessionConfig : ClusterConfig {}

public class Session : ISession
{
    public string Id => _cluster.Id;

    private ICluster _cluster;

    private IClusterProperties? _properties;

    private ILoggerFactory? _loggerFactory;

    public IClusterProperties ClusterProperties => _properties ??
        throw new InvalidOperationException("GetClusterPropertiesAsync must be called before getting ClusterProperties.");

    private Session(ICluster cluster, ILoggerFactory? loggerFactory = null)
    {
        _cluster = cluster;
        _loggerFactory = loggerFactory;
    }

    private async Task GetClusterPropertiesAsync()
    {
        _properties = await _cluster.GetPropertiesAsync();
    }

    //TODO: Support Storage queue
    private IMessageQueue CreateQueueClient(bool sender)
    {
        Debug.Assert(_properties != null);

        var queueType = _properties.QueueProperties?.QueueType;
        if (!ServiceBusQueue.QueueType.Equals(queueType))
        {
            throw new NotImplementedException($"Queue type {queueType} is not supported.");
        }

        var queueOptions = new ServiceBusQueueOptions()
        {
            QueueType = queueType,
            ConnectionString = _properties.QueueProperties!.ConnectionString,
            QueueName = sender ? _properties.QueueProperties.RequestQueueName : _properties.QueueProperties.ResponseQueueName,
            MessageLease = 60, //TODO: Make it configurable in ClusterConfig
            RetryOnThrottled = true
        };
        var logger = _loggerFactory?.CreateLogger<ServiceBusQueue>();
        return new ServiceBusQueue(queueOptions, logger);
    }

    public IMessageQueue CreateSender()
    {
        return CreateQueueClient(true);
    }

    public IMessageQueue CreateReceiver()
    {
        return CreateQueueClient(false);
    }

    public static async Task<Session> CreateOrUpdateAsync(TokenCredential credential, SessionConfig sessionConfig, string? sessionId = null,
        ILoggerFactory? loggerFactory = null, CancellationToken token = default)
    {
        var logger = loggerFactory?.CreateLogger<Cluster>();
        var cluster = new Cluster(credential, sessionConfig, sessionId, logger);
        await cluster.CreateOrUpdateAsync(token);
        var session = new Session(cluster, loggerFactory);
        await session.GetClusterPropertiesAsync();
        return session;
    }

    public static async Task<Session> GetAsync(TokenCredential credential, string sessionId,
        ILoggerFactory? loggerFactory = null, CancellationToken token = default)
    {
        var logger = loggerFactory?.CreateLogger<Cluster>();
        var cluster = new Cluster(credential, sessionId, logger);
        await cluster.ValidateAsync(token);
        var session = new Session(cluster, loggerFactory);
        await session.GetClusterPropertiesAsync();
        return session;
    }

    public static async Task DestroyAsync(TokenCredential credential, string sessionId,
        ILoggerFactory? loggerFactory = null, CancellationToken token = default)
    {
        var logger = loggerFactory?.CreateLogger<Cluster>();
        var cluster = new Cluster(credential, sessionId, logger);
        await cluster.DestroyAsync(token);
    }
}
