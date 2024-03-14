using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Cloud.Soa;

interface IResponseQueue : IMessageQueue
{
    const string ConfigName = "Responses";
}

class ResponseStorageQueue : StorageQueue, IResponseQueue
{
    public ResponseStorageQueue(IOptionsMonitor<StorageQueueOptions> options) : base(options.Get(IResponseQueue.ConfigName)) {}
}

class ResponseServiceBusQueue: ServiceBusQueue, IResponseQueue
{
    public ResponseServiceBusQueue(IOptionsMonitor<ServiceBusQueueOptions> options, ILogger<ResponseServiceBusQueue> logger)
        : base(options.Get(IResponseQueue.ConfigName), logger) { }
}

static class ServiceCollectionResponseQueueExtensions
{
    public static IServiceCollection AddResponseQueue(this IServiceCollection services, IConfiguration configuration)
    {
        var queueType = configuration["Queues:QueueType"];

        if (string.IsNullOrEmpty(queueType) ||
            ServiceBusQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<IResponseQueue>(provider =>
            {
                var option = provider.GetRequiredService<IOptionsMonitor<ServiceBusQueueOptions>>();
                var logger = provider.GetRequiredService<ILogger<ResponseServiceBusQueue>>();
                return new ResponseServiceBusQueue(option, logger);
            });
            services.AddQueueOptions<ServiceBusQueueOptions>(configuration, IResponseQueue.ConfigName);
        }
        else if (StorageQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<IResponseQueue>(provider =>
            {
                var option = provider.GetRequiredService<IOptionsMonitor<StorageQueueOptions>>();
                return new ResponseStorageQueue(option);
            });
            services.AddQueueOptions<StorageQueueOptions>(configuration, IResponseQueue.ConfigName);
        }
        else
        {
            throw new ArgumentException($"Invalid queue type '{queueType}'!");
        }

        return services;
    }
}
