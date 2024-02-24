using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa;

class QueueRequest : IQueueRequest
{
    private readonly QueueClient _client;
    private readonly QueueMessage _message;

    public QueueRequest(QueueClient client, QueueMessage message)
    {
        _client = client;
        _message = message;
    }

    public string Id => _message.MessageId;

    public string Message => _message.MessageText;

    public Task RenewLeaseAsync(TimeSpan lease)
    {
        return _client.UpdateMessageAsync(_message.MessageId, _message.PopReceipt, visibilityTimeout: lease);
    }

    public Task DeleteAsync()
    {
        return _client.DeleteMessageAsync(_message.MessageId, _message.PopReceipt);
    }
}

class RequestQueueOptions
{
    [Required]
    public string? QueueName { get; set; }

    [Required]
    public string? ConnectionString { get; set; }
}

class RequestQueue : IRequestQueue
{
    private readonly ILogger _logger;
    private readonly RequestQueueOptions _options;
    private QueueClient _client;

    public RequestQueue(ILogger<RequestQueue> logger, IOptions<RequestQueueOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _client = new QueueClient(_options.ConnectionString, _options.QueueName);
    }

    public async Task<IQueueRequest> ReceiveAsync(TimeSpan lease = default, CancellationToken cancel = default)
    {
        var message = await _client.ReceiveMessageAsync(lease, cancel);
        if (message.Value == null)
        {
            throw new IRequestQueue.NoRequest();
        }
        return new QueueRequest(_client, message);
    }

    public async Task<IQueueRequest> WaitAsync(TimeSpan lease = default, CancellationToken cancel = default)
    {
        while (true)
        {
            try
            {
                return await ReceiveAsync(lease, cancel);
            }
            catch (IRequestQueue.NoRequest)
            {
                await Task.Delay(1000, cancel);
                continue;
            }
        }
    }
}

static class ServiceCollectionRequestQueueExtensions
{
    public static IServiceCollection AddRequestQueue(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IRequestQueue, RequestQueue>();
        services.AddOptionsWithValidateOnStart<RequestQueueOptions>()
            .Bind(configuration.GetSection("Requests"))
            .ValidateDataAnnotations();
        return services;
    }
}
