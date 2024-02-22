using System.Threading.Tasks;

namespace Cloud.Soa;

class QueueResponses : IQueueResponses
{
    public Task SendAsync(IQueueResponse response)
    {
        throw new System.NotImplementedException();
    }

    public Task SendAsync(string response)
    {
        throw new System.NotImplementedException();
    }
}
