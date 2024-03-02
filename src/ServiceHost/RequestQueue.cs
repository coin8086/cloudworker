using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Cloud.Soa;

interface IRequestQueue : IMessageQueue
{
    const string ConfigName = "Requests";
}

class RequestStorageQueue : StorageQueue, IRequestQueue
{
    public RequestStorageQueue(IOptionsMonitor<QueueOptions> options) : base(options.Get(IRequestQueue.ConfigName)) {}
}

class RequestServiceBusQueue : ServiceBusQueue, IRequestQueue
{
    public RequestServiceBusQueue(IOptionsMonitor<QueueOptions> options) : base(options.Get(IRequestQueue.ConfigName)) {}
}

static class ServiceCollectionRequestQueueExtensions
{
    public static IServiceCollection AddRequestQueue(this IServiceCollection services, IConfiguration configuration)
    {
        var config = configuration.GetSection(IRequestQueue.ConfigName);
        var queueType = config["QueueType"];

        services.AddTransient<IRequestQueue>(provider => {
            if (string.IsNullOrEmpty(queueType) ||
                ServiceBusQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
            {
                var option = provider.GetRequiredService<IOptionsMonitor<QueueOptions>>();
                return new RequestServiceBusQueue(option);
            }
            else if (StorageQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
            {
                var option = provider.GetRequiredService<IOptionsMonitor<QueueOptions>>();
                return new RequestStorageQueue(option);
            }
            else
            {
                throw new ArgumentException($"Invalid queue type '{queueType}'!");
            }
        });

        services.AddOptionsWithValidateOnStart<QueueOptions>(IRequestQueue.ConfigName)
            .Bind(configuration.GetSection(IRequestQueue.ConfigName))
            .ValidateDataAnnotations();
        return services;
    }
}
