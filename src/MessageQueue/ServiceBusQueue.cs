using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
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

public class ServiceBusQueue : IMessageQueue, IAsyncDisposable
{
    public const string QueueType = "servicebus";

    private readonly QueueOptions _options;
    private readonly TimeSpan _messageLease;
    private ServiceBusClient _client;
    private ServiceBusReceiver _receiver;
    private ServiceBusSender _sender;
    private ILogger? _logger;

    public ServiceBusQueue(QueueOptions options, ILogger? logger = null)
    {
        _options = options;
        if (_options.MessageLease is null)
        {
            throw new ArgumentNullException(nameof(options.MessageLease));
        }
        _messageLease = TimeSpan.FromSeconds((double)_options.MessageLease);

        _client = new ServiceBusClient(_options.ConnectionString);
        //TODO: Shall we have either receiver or sender, but not both, for an instance of this class?
        _receiver = _client.CreateReceiver(_options.QueueName);
        _sender = _client.CreateSender(_options.QueueName);

        _logger = logger;
    }

    public TimeSpan MessageLease => _messageLease;

    public async Task<IMessage> WaitAsync(bool retryOnThrottled = false, CancellationToken cancel = default)
    {
        ServiceBusReceivedMessage? message = null;
        await RetryWhenThrottled(async () =>
        {
            message = await _receiver.ReceiveMessageAsync(TimeSpan.MaxValue, cancel).ConfigureAwait(false);
        }, retryOnThrottled, cancel).ConfigureAwait(false);
        return new ServiceBusQueueMessage(message!, _receiver);
    }

    public async Task SendAsync(string message, bool retryOnThrottled = false, CancellationToken cancel = default)
    {
        await RetryWhenThrottled(async () =>
        {
            await _sender.SendMessageAsync(new ServiceBusMessage(message), cancel).ConfigureAwait(false);
        }, retryOnThrottled, cancel).ConfigureAwait(false);
    }

    private async Task RetryWhenThrottled(Func<Task> task, bool retryOnThrottled = false, CancellationToken cancel = default)
    {
        while (!cancel.IsCancellationRequested)
        {
            try
            {
                await task().ConfigureAwait(false);
                break;
            }
            catch (ServiceBusException ex)
            {
                if (retryOnThrottled && ex.Reason == ServiceBusFailureReason.ServiceBusy)
                {
                    _logger?.LogWarning(ex, "ServiceBusQueue: Being throttled. Sleep 2 seconds before retry.");
                    await Task.Delay(TimeSpan.FromSeconds(2), cancel).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }
}
