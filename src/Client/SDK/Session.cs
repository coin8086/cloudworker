using Azure.Core;
using CloudWorker.MessageQueue;
using Microsoft.Extensions.Logging;
using System;
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
    public string Id => _cluster.Id.ToString();

    private Cluster _cluster;

    private Session(Cluster cluster)
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
        ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<Cluster>();
        var cluster = new Cluster(credential, sessionConfig, sessionId, logger);
        await cluster.CreateOrUpdateAsync();
        return new Session(cluster);
    }

    public static async Task<Session> GetAsync(TokenCredential credential, string sessionId, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<Cluster>();
        var cluster = new Cluster(credential, sessionId, logger);
        await cluster.GetAsync();
        return new Session(cluster);
    }

    public static async Task DestroyAsync(TokenCredential credential, string sessionId, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<Cluster>();
        var cluster = new Cluster(credential, sessionId, logger);
        await cluster.DestroyAsync();
    }
}
