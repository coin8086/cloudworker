using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa;

class QueueRequests : IQueueRequests
{
    public Task<IQueueRequest> ReceiveAsync(TimeSpan lease = default, CancellationToken cancel = default)
    {
        throw new NotImplementedException();
    }
}
