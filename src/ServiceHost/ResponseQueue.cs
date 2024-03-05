using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    public ResponseServiceBusQueue(IOptionsMonitor<QueueOptions> options) : base(options.Get(IResponseQueue.ConfigName)) { }
}

static class ServiceCollectionResponseQueueExtensions
{
    public static IServiceCollection AddResponseQueue(this IServiceCollection services, IConfiguration configuration)
    {
        var config = configuration.GetSection(IResponseQueue.ConfigName);
        var queueType = config["QueueType"];

        if (string.IsNullOrEmpty(queueType) ||
            ServiceBusQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<IResponseQueue>(provider =>
            {
                var option = provider.GetRequiredService<IOptionsMonitor<QueueOptions>>();
                return new ResponseServiceBusQueue(option);
            });
            services.AddOptionsWithValidateOnStart<QueueOptions>(IResponseQueue.ConfigName)
                .Bind(configuration.GetSection(IResponseQueue.ConfigName))
                .ValidateDataAnnotations();
        }
        else if (StorageQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<IResponseQueue>(provider =>
            {
                var option = provider.GetRequiredService<IOptionsMonitor<StorageQueueOptions>>();
                return new ResponseStorageQueue(option);
            });
            services.AddOptionsWithValidateOnStart<StorageQueueOptions>(IResponseQueue.ConfigName)
                .Bind(configuration.GetSection(IResponseQueue.ConfigName))
                .ValidateDataAnnotations();
        }
        else
        {
            throw new ArgumentException($"Invalid queue type '{queueType}'!");
        }

        return services;
    }
}
