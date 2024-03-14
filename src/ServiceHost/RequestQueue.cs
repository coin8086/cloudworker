using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Cloud.Soa;

interface IRequestQueue : IMessageQueue
{
    const string ConfigName = "Requests";
}

class RequestStorageQueue : StorageQueue, IRequestQueue
{
    public RequestStorageQueue(IOptionsMonitor<StorageQueueOptions> options) : base(options.Get(IRequestQueue.ConfigName)) {}
}

class RequestServiceBusQueue : ServiceBusQueue, IRequestQueue
{
    public RequestServiceBusQueue(IOptionsMonitor<ServiceBusQueueOptions> options, ILogger<RequestServiceBusQueue> logger)
        : base(options.Get(IRequestQueue.ConfigName), logger) {}
}

static class ServiceCollectionRequestQueueExtensions
{
    public static IServiceCollection AddRequestQueue(this IServiceCollection services, IConfiguration configuration)
    {
        var queueType = configuration["Queues:QueueType"];

        if (string.IsNullOrEmpty(queueType) ||
            ServiceBusQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<IRequestQueue>(provider =>
            {
                var option = provider.GetRequiredService<IOptionsMonitor<ServiceBusQueueOptions>>();
                var logger = provider.GetRequiredService<ILogger<RequestServiceBusQueue>>();
                return new RequestServiceBusQueue(option, logger);
            });
            services.AddQueueOptions<ServiceBusQueueOptions>(configuration, IRequestQueue.ConfigName);
        }
        else if (StorageQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<IRequestQueue>(provider =>
            {
                var option = provider.GetRequiredService<IOptionsMonitor<StorageQueueOptions>>();
                return new RequestStorageQueue(option);
            });
            services.AddQueueOptions<StorageQueueOptions>(configuration, IRequestQueue.ConfigName);
        }
        else
        {
            throw new ArgumentException($"Invalid queue type '{queueType}'!");
        }

        return services;
    }
}
