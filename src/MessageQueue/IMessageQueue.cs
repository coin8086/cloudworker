using System.Threading.Tasks;
using System;
using System.Threading;

namespace Cloud.Soa;

public interface IMessage
{
    string Id { get; }

    string Content { get; }

    //Renew with the default lease on IMessageQueue
    Task RenewLeaseAsync();

    Task ReturnAsync();

    Task DeleteAsync();
}

public interface IMessageQueue
{
    //Default message lease for the queue
    TimeSpan MessageLease { get; }

    //Wait for a message, until one is received or the operation is cancelled.
    Task<IMessage> WaitAsync(CancellationToken cancel = default);

    Task SendAsync(string message);
}
