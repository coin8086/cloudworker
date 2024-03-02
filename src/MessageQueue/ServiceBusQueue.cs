using Azure.Messaging.ServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa;

public class ServiceBusQueueMessage : IMessage
{
    private readonly ServiceBusReceivedMessage _message;

    private readonly ServiceBusReceiver _receiver;

    public ServiceBusQueueMessage(ServiceBusReceivedMessage message, ServiceBusReceiver receiver)
    {
        _message = message;
        _receiver = receiver;
    }

    public string Id => _message.MessageId;

    public string Content => _message.Body.ToString();

    public Task RenewLeaseAsync()
    {
        return _receiver.RenewMessageLockAsync(_message);
    }

    public Task ReturnAsync()
    {
        return _receiver.AbandonMessageAsync(_message);
    }

    public Task DeleteAsync()
    {
        return _receiver.CompleteMessageAsync(_message);
    }
}

//TODO: Implement IAsyncDisposable since ServiceBusClient has it.
public class ServiceBusQueue : IMessageQueue
{
    private readonly QueueOptions _options;
    private ServiceBusClient _client;
    private ServiceBusReceiver _receiver;
    private ServiceBusSender _sender;

    public ServiceBusQueue(QueueOptions options)
    {
        _options = options;
        _client = new ServiceBusClient(_options.ConnectionString);
        //TODO: Shall we have either receiver or sender, but not both, for an instance of this class?
        _receiver = _client.CreateReceiver(_options.QueueName);
        _sender = _client.CreateSender(_options.QueueName);
    }

    public TimeSpan MessageLease => throw new NotImplementedException();

    public async Task<IMessage> WaitAsync(CancellationToken? cancel = null)
    {
        var message = await _receiver.ReceiveMessageAsync(TimeSpan.MaxValue, cancel ?? CancellationToken.None);
        return new ServiceBusQueueMessage(message, _receiver);
    }

    public Task SendAsync(string message)
    {
        return _sender.SendMessageAsync(new ServiceBusMessage(message));
    }
}
