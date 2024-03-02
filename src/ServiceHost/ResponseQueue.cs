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
    public ResponseStorageQueue(IOptionsMonitor<QueueOptions> options) : base(options.Get(IResponseQueue.ConfigName)) {}
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

        services.AddTransient<IResponseQueue>(provider =>
        {
            if (string.IsNullOrEmpty(queueType) ||
                ServiceBusQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
            {
                var option = provider.GetRequiredService<IOptionsMonitor<QueueOptions>>();
                return new ResponseServiceBusQueue(option);
            }
            else if (StorageQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
            {
                var option = provider.GetRequiredService<IOptionsMonitor<QueueOptions>>();
                return new ResponseStorageQueue(option);
            }
            else
            {
                throw new ArgumentException($"Invalid queue type '{queueType}'!");
            }
        });

        services.AddOptionsWithValidateOnStart<QueueOptions>(IResponseQueue.ConfigName)
            .Bind(configuration.GetSection(IResponseQueue.ConfigName))
            .ValidateDataAnnotations();
        return services;
    }
}
