using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Cloud.Soa;

class QueueResponsesOptions
{
    [Required]
    public string? QueueName { get; set; }

    [Required]
    public string? ConnectionString { get; set; }
}

class QueueResponses : IQueueResponses
{
    private readonly ILogger _logger;
    private readonly QueueResponsesOptions _options;
    private QueueClient _client;

    public QueueResponses(ILogger<QueueResponses> logger, IOptions<QueueResponsesOptions> options)
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

static class ServiceCollectionQueueResponsesExtensions
{
    public static IServiceCollection AddQueueResponses(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IQueueResponses, QueueResponses>();
        services.AddOptionsWithValidateOnStart<QueueResponsesOptions>()
            .Bind(configuration.GetSection("Responses"))
            .ValidateDataAnnotations();
        return services;
    }
}
