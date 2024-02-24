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

class QueueRequestsOptions
{
    [Required]
    public string? QueueName { get; set; }

    [Required]
    public string? ConnectionString { get; set; }
}

class QueueRequests : IQueueRequests
{
    private readonly ILogger _logger;
    private readonly QueueRequestsOptions _options;
    private QueueClient _client;

    public QueueRequests(ILogger<QueueRequests> logger, IOptions<QueueRequestsOptions> options)
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
            throw new NoQueueRequest();
        }
        return new QueueRequest(_client, message);
    }
}

static class ServiceCollectionQueueRequestsExtensions
{
    public static IServiceCollection AddQueueRequests(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IQueueRequests, QueueRequests>();
        services.AddOptionsWithValidateOnStart<QueueRequestsOptions>()
            .Bind(configuration.GetSection("Requests"))
            .ValidateDataAnnotations();
        return services;
    }
}
