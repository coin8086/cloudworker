using Azure.Core;
using System;
using System.Threading.Tasks;

namespace CloudWorker.Client.SDK;

public interface ICluster
{
    string Id { get; }

    Task<QueueProperties> GetQueueProperties();

    Task DestroyAsync();
}

public class Cluster : ICluster
{
    public string Id { get; private set; } = string.Empty;

    public Cluster() {}

    public static Task<Cluster> CreateAsync(ClusterConfig clusterConfig, TokenCredential? credential = default)
    {
        throw new NotImplementedException();
    }

    public static Task<Cluster> GetAsync(string id, TokenCredential? credential = default)
    {
        throw new NotImplementedException();
    }

    public static async Task DestroyAsync(string id, TokenCredential? credential = default)
    {
        var cluster = await GetAsync(id, credential);
        await cluster.DestroyAsync();
    }

    public Task DestroyAsync()
    {
        throw new NotImplementedException();
    }

    public Task<QueueProperties> GetQueueProperties()
    {
        throw new NotImplementedException();
    }
}
