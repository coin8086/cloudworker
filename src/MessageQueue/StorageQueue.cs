using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWork.MessageQueue;

public class StorageQueueMessage : IMessage
{
    private readonly QueueClient _client;
    private readonly QueueMessage _message;
    private readonly TimeSpan _lease;

    public StorageQueueMessage(QueueClient client, QueueMessage message, TimeSpan lease)
    {
        _client = client;
        _message = message;
        _lease = lease;
    }

    public string Id => _message.MessageId;

    public string Content => _message.MessageText;

    public Task RenewLeaseAsync()
    {
        return _client.UpdateMessageAsync(_message.MessageId, _message.PopReceipt, visibilityTimeout: _lease);
    }

    public Task ReturnAsync()
    {
        return _client.UpdateMessageAsync(_message.MessageId, _message.PopReceipt, visibilityTimeout: TimeSpan.Zero);
    }

    public Task DeleteAsync()
    {
        return _client.DeleteMessageAsync(_message.MessageId, _message.PopReceipt);
    }
}

//NOTE: Storage Queue client has built-in retry policies by QueueClientOptions' properties Retry and RetryPolicy.
//See https://learn.microsoft.com/en-us/dotnet/api/azure.storage.queues.queueclientoptions?view=azure-dotnet
public class StorageQueueOptions : QueueOptions
{
    public int? QueryInterval { get; set; } = 500;  //In milliseconds.

    public static StorageQueueOptions Default { get; } = new StorageQueueOptions();

    public override void Merge(QueueOptions? other)
    {
        base.Merge(other);
        if (other is StorageQueueOptions opts)
        {
            if (opts.QueryInterval != null)
            {
                QueryInterval = opts.QueryInterval;
            }
        }
    }

    public override bool Validate()
    {
        return base.Validate() && (QueryInterval is null || QueryInterval >= 0);
    }
}

public class StorageQueue : IMessageQueue
{
    public const string QueueType = "storage";

    private readonly StorageQueueOptions _options;
    private readonly TimeSpan _messageLease;
    private QueueClient _client;

    public StorageQueue(StorageQueueOptions options)
    {
        _options = options;
        if (_options.MessageLease is null)
        {
            throw new ArgumentException();
        }
        _messageLease = TimeSpan.FromSeconds((double)_options.MessageLease);
        _client = new QueueClient(_options.ConnectionString, _options.QueueName);
    }

    public TimeSpan MessageLease => _messageLease;

    public async Task<IMessage> WaitAsync(CancellationToken cancel = default)
    {
        var messages = await WaitBatchAsync(1, cancel).ConfigureAwait(false);
        return messages[0];
    }

    public async Task<IReadOnlyList<IMessage>> WaitBatchAsync(int batchSize, CancellationToken cancel = default)
    {
        var delay = _options.QueryInterval ?? StorageQueueOptions.Default.QueryInterval;
        while (true)
        {
            var messages = await _client.ReceiveMessagesAsync(batchSize, MessageLease, cancel).ConfigureAwait(false);
            if (messages.Value == null || messages.Value.Length == 0)
            {
                await Task.Delay(delay!.Value, cancel).ConfigureAwait(false);
            }
            else
            {
                return messages.Value.Select(msg => new StorageQueueMessage(_client, msg, MessageLease)).ToImmutableList();
            }
        }
    }

    public Task SendAsync(string message, CancellationToken cancel = default)
    {
        return _client.SendMessageAsync(message, MessageLease, cancellationToken: cancel);
    }
}
