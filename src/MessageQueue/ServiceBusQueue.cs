using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa.MessageQueue;

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

    //TODO: Retry on throttled for the following methods
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

public class ServiceBusQueueOptions : QueueOptions
{
    public bool? RetryOnThrottled { get; set; } = true;

    public static ServiceBusQueueOptions Default { get; } = new ServiceBusQueueOptions();

    public override void Merge(QueueOptions? other)
    {
        base.Merge(other);
        if (other is ServiceBusQueueOptions opts)
        {
            if (opts.RetryOnThrottled != null)
            {
                RetryOnThrottled = opts.RetryOnThrottled;
            }
        }
    }
}

public class ServiceBusQueue : IMessageQueue, IAsyncDisposable
{
    public const string QueueType = "servicebus";

    private readonly ServiceBusQueueOptions _options;
    private readonly TimeSpan _messageLease;
    private ServiceBusClient _client;
    private ServiceBusReceiver _receiver;
    private ServiceBusSender _sender;
    private ILogger? _logger;

    public ServiceBusQueue(ServiceBusQueueOptions options, ILogger? logger = null)
    {
        _options = options;
        if (_options.MessageLease is null)
        {
            throw new ArgumentNullException(nameof(options.MessageLease));
        }
        _messageLease = TimeSpan.FromSeconds((double)_options.MessageLease);

        _options.RetryOnThrottled ??= ServiceBusQueueOptions.Default.RetryOnThrottled;

        _client = new ServiceBusClient(_options.ConnectionString);

        //TODO: Shall we have either receiver or sender, but not both, for an instance of this class?
        _receiver = _client.CreateReceiver(_options.QueueName);
        _sender = _client.CreateSender(_options.QueueName);

        _logger = logger;
    }

    public TimeSpan MessageLease => _messageLease;

    public async Task<IMessage> WaitAsync(CancellationToken cancel = default)
    {
        var messages = await WaitBatchAsync(1, cancel).ConfigureAwait(false);
        return messages[0];
    }

    public async Task<IReadOnlyList<IMessage>> WaitBatchAsync(int batchSize, CancellationToken cancel = default)
    {
        IReadOnlyList<ServiceBusReceivedMessage>? messages = null;
        while (true)
        {
            await RetryWhenThrottled(async () =>
            {
                messages = await _receiver.ReceiveMessagesAsync(batchSize, TimeSpan.MaxValue, cancel).ConfigureAwait(false);
            }, cancel).ConfigureAwait(false);
            if (messages?.Count > 0)
            {
                break;
            }
            else
            {
                _logger?.LogWarning("Service Bus ReceiveMessagesAsync returns empty result!");
            }
        }
        return messages!.Select(msg => new ServiceBusQueueMessage(msg, _receiver)).ToImmutableList();
    }

    public async Task SendAsync(string message, CancellationToken cancel = default)
    {
        await RetryWhenThrottled(async () =>
        {
            await _sender.SendMessageAsync(new ServiceBusMessage(message), cancel).ConfigureAwait(false);
        }, cancel).ConfigureAwait(false);
    }

    private async Task RetryWhenThrottled(Func<Task> task, CancellationToken cancel = default)
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
                if (_options.RetryOnThrottled!.Value && ex.Reason == ServiceBusFailureReason.ServiceBusy)
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
