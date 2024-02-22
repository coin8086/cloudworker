using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Cloud.Soa;

class QueueResponsesOptions
{
    public string QueueName { get; set; }

    public string ConnectionString { get; set; }
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
