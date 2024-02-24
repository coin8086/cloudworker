using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cloud.Soa;

interface IResponseQueue : IMessageQueue
{
}

class ResponseQueue : MessageQueue, IResponseQueue
{
    public ResponseQueue(IOptionsMonitor<QueueOptions> options) : base(options.Get("Responses")) {}
}

static class ServiceCollectionResponseQueueExtensions
{
    public static IServiceCollection AddResponseQueue(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IResponseQueue, ResponseQueue>();
        services.AddOptionsWithValidateOnStart<QueueOptions>("Responses")
            .Bind(configuration.GetSection("Responses"))
            .ValidateDataAnnotations();
        return services;
    }
}
