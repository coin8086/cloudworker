using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa;

public class Message : IMessage
{
    private readonly QueueClient _client;
    private readonly QueueMessage _message;

    public Message(QueueClient client, QueueMessage message)
    {
        _client = client;
        _message = message;
    }

    public string Id => _message.MessageId;

    public string Content => _message.MessageText;

    public Task RenewLeaseAsync(TimeSpan lease)
    {
        return _client.UpdateMessageAsync(_message.MessageId, _message.PopReceipt, visibilityTimeout: lease);
    }

    public Task DeleteAsync()
    {
        return _client.DeleteMessageAsync(_message.MessageId, _message.PopReceipt);
    }
}

public class QueueOptions
{
    [Required]
    public string? QueueName { get; set; }

    [Required]
    public string? ConnectionString { get; set; }
}

public class MessageQueue : IMessageQueue
{
    private readonly QueueOptions _options;
    private QueueClient _client;

    public MessageQueue(QueueOptions options)
    {
        _options = options;
        _client = new QueueClient(_options.ConnectionString, _options.QueueName);
    }

    public async Task<IMessage> ReceiveAsync(TimeSpan? lease = default, CancellationToken? cancel = default)
    {
        var message = await _client.ReceiveMessageAsync(lease, cancel ?? CancellationToken.None);
        if (message.Value == null)
        {
            throw new IMessageQueue.NoMessage();
        }
        return new Message(_client, message);
    }

    public async Task<IMessage> WaitAsync(TimeSpan? lease = default, TimeSpan? interval = default, CancellationToken? cancel = default)
    {
        var delay = interval?.Microseconds ?? 200;
        while (true)
        {
            try
            {
                return await ReceiveAsync(lease, cancel);
            }
            catch (IMessageQueue.NoMessage)
            {
                await Task.Delay(delay, cancel ?? CancellationToken.None);
                continue;
            }
        }
    }

    public Task SendAsync(string message)
    {
        return _client.SendMessageAsync(message);
    }
}
