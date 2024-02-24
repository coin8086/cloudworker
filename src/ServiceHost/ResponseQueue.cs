using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Cloud.Soa;

class ResponseQueueOptions
{
    [Required]
    public string? QueueName { get; set; }

    [Required]
    public string? ConnectionString { get; set; }
}

class ResponseQueue : IResponseQueue
{
    private readonly ILogger _logger;
    private readonly ResponseQueueOptions _options;
    private QueueClient _client;

    public ResponseQueue(ILogger<ResponseQueue> logger, IOptions<ResponseQueueOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _client = new QueueClient(_options.ConnectionString, _options.QueueName);
    }

    public Task SendAsync(string response)
    {
        return _client.SendMessageAsync(response);
    }
}

static class ServiceCollectionResponseQueueExtensions
{
    public static IServiceCollection AddResponseQueue(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IResponseQueue, ResponseQueue>();
        services.AddOptionsWithValidateOnStart<ResponseQueueOptions>()
            .Bind(configuration.GetSection("Responses"))
            .ValidateDataAnnotations();
        return services;
    }
}
