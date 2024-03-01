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
    public class NoMessage : ApplicationException { }

    //Default lease for the whole queue
    TimeSpan MessageLease { get; }

    Task<IMessage> ReceiveAsync(CancellationToken? cancel = default);

    Task<IMessage> WaitAsync(CancellationToken? cancel = default);

    Task SendAsync(string message);
}
