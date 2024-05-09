using Azure.Core;
using CloudWorker.MessageQueue;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker.Client.SDK;

public interface ISession
{
    string Id { get; }

    IMessageQueue CreateSender();

    IMessageQueue CreateReceiver();
}

public class SessionConfig : ClusterConfig {}

public class Session : ISession
{
    public string Id => _cluster.Id;

    private ICluster _cluster;

    private Session(ICluster cluster)
    {
        _cluster = cluster;
    }

    public IMessageQueue CreateSender()
    {
        throw new NotImplementedException();
    }

    public IMessageQueue CreateReceiver()
    {
        throw new NotImplementedException();
    }

    public static async Task<Session> CreateOrUpdateAsync(TokenCredential credential, SessionConfig sessionConfig, string? sessionId = null,
        ILoggerFactory? loggerFactory = null, CancellationToken token = default)
    {
        var logger = loggerFactory?.CreateLogger<Cluster>();
        var cluster = new Cluster(credential, sessionConfig, sessionId, logger);
        await cluster.CreateOrUpdateAsync(token);
        return new Session(cluster);
    }

    public static async Task<Session> GetAsync(TokenCredential credential, string sessionId,
        ILoggerFactory? loggerFactory = null, CancellationToken token = default)
    {
        var logger = loggerFactory?.CreateLogger<Cluster>();
        var cluster = new Cluster(credential, sessionId, logger);
        await cluster.ValidateAsync(token);
        return new Session(cluster);
    }

    public static async Task DestroyAsync(TokenCredential credential, string sessionId,
        ILoggerFactory? loggerFactory = null, CancellationToken token = default)
    {
        var logger = loggerFactory?.CreateLogger<Cluster>();
        var cluster = new Cluster(credential, sessionId, logger);
        await cluster.DestroyAsync(token);
    }
}
