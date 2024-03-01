using System.Threading.Tasks;
using System;
using System.Threading;

namespace Cloud.Soa;

public interface IMessage
{
    string Id { get; }

    string Content { get; }

    Task RenewLeaseAsync(TimeSpan lease);

    Task DeleteAsync();
}

public interface IMessageQueue
{
    public class NoMessage : ApplicationException { }

    Task<IMessage> ReceiveAsync(TimeSpan? lease = default, CancellationToken? cancel = default);

    Task<IMessage> WaitAsync(TimeSpan? lease = default, CancellationToken? cancel = default);

    Task SendAsync(string message);
}
