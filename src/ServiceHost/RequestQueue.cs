using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cloud.Soa;

interface IRequestQueue : IMessageQueue
{
}

class RequestQueue : StorageQueue, IRequestQueue
{
    public RequestQueue(IOptionsMonitor<QueueOptions> options) : base(options.Get("Requests")) {}
}

static class ServiceCollectionRequestQueueExtensions
{
    public static IServiceCollection AddRequestQueue(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IRequestQueue, RequestQueue>();
        services.AddOptionsWithValidateOnStart<QueueOptions>("Requests")
            .Bind(configuration.GetSection("Requests"))
            .ValidateDataAnnotations();
        return services;
    }
}
