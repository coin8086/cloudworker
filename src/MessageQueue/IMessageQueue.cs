using System.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;

namespace CloudWorker.MessageQueue;

public interface IMessage
{
    string Id { get; }

    string Content { get; }

    //Renew with the default lease on IMessageQueue
    Task RenewLeaseAsync();

    //Return to queue
    Task ReturnAsync();

    //Delete from queue
    Task DeleteAsync();
}

public interface IMessageQueue
{
    //Default message lease for the queue
    TimeSpan MessageLease { get; }

    //Wait for a message, until one is received or the operation is cancelled.
    Task<IMessage> WaitAsync(CancellationToken cancel = default);

    //Wait for a batch of messages. The number of returned messages may be less than batchSize.
    Task<IReadOnlyList<IMessage>> WaitBatchAsync(int batchSize, CancellationToken cancel = default);

    //Send a message to queue
    Task SendAsync(string message, CancellationToken cancel = default);
}
