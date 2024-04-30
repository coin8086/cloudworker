using CloudWorker.MessageQueue;
using System;
using System.Threading.Tasks;

namespace CloudWorker.Client.SDK;

public interface ISession
{
    string Id { get; }

    IMessageQueue CreateSender();

    IMessageQueue CreateReceiver();

    Task DestroyAsync();
}

public class Session : ISession
{
    public string Id { get; }

    protected Cluster _cluster;

    protected Session(Cluster cluster)
    {
        _cluster = cluster;
        Id = cluster.Id;
    }

    public static async Task<Session> CreateAsync(ClusterConfig clusterConfig)
    {
        var cluster = await Cluster.CreateAsync(clusterConfig);
        return new Session(cluster);
    }

    public static async Task<Session> GetAsync(string id)
    {
        var cluster = await Cluster.GetAsync(id);
        return new Session(cluster);
    }

    public static async Task DestroyAsync(string id)
    {
        var session = await GetAsync(id);
        await session.DestroyAsync();
    }

    public async Task DestroyAsync()
    {
        await _cluster.DestroyAsync();
    }

    public IMessageQueue CreateSender()
    {
        throw new NotImplementedException();
    }

    public IMessageQueue CreateReceiver()
    {
        throw new NotImplementedException();
    }
}
