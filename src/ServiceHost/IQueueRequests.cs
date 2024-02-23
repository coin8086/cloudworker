using System.Threading.Tasks;
using System.Threading;
using System;

namespace Cloud.Soa;

interface IQueueRequest
{
    string Id { get; }

    string Message { get; }

    Task RenewLeaseAsync(TimeSpan lease);

    Task RemoveAsync();
}

interface IQueueRequests
{
    Task<IQueueRequest> ReceiveAsync(TimeSpan lease = default, CancellationToken cancel = default);
}

class NoQueueRequest : ApplicationException
{
}