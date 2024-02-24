using System.Threading.Tasks;
using System.Threading;
using System;

namespace Cloud.Soa;

interface IQueueRequest
{
    string Id { get; }

    string Message { get; }

    Task RenewLeaseAsync(TimeSpan lease);

    Task DeleteAsync();
}

interface IQueueRequests
{
    class NoRequest : ApplicationException {}

    Task<IQueueRequest> ReceiveAsync(TimeSpan lease = default, CancellationToken cancel = default);

    Task<IQueueRequest> WaitAsync(TimeSpan lease = default, CancellationToken cancel = default);
}

