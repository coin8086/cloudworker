using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa;

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

public class StorageQueueOptions : QueueOptions
{
    public int? QueryInterval { get; set; } = 200;  //In milliseconds.

    public static StorageQueueOptions Default { get; } = new StorageQueueOptions();
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
        var delay = _options.QueryInterval ?? StorageQueueOptions.Default.QueryInterval;
        while (true)
        {
            var message = await _client.ReceiveMessageAsync(MessageLease, cancel);
            if (message.Value == null)
            {
                await Task.Delay(delay!.Value, cancel);
            }
            else
            {
                return new StorageQueueMessage(_client, message, MessageLease);
            }
        }
    }

    public Task SendAsync(string message)
    {
        return _client.SendMessageAsync(message);
    }
}
