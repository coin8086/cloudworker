using System.Threading.Tasks;
using System.Threading;
using System;

namespace Cloud.Soa;

interface IQueueRequest
{
    string Id { get; }

    string Content { get; }

    Task RenewLeaseAsync(TimeSpan lease);

    Task DeleteAsync();
}

interface IRequestQueue
{
    class NoMessage : ApplicationException {}

    Task<IQueueRequest> ReceiveAsync(TimeSpan lease = default, CancellationToken cancel = default);

    Task<IQueueRequest> WaitAsync(TimeSpan lease = default, CancellationToken cancel = default);
}

