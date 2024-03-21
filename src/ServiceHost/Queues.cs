using Cloud.Soa.MessageQueue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Cloud.Soa.ServiceHost;

static class Queues
{
    //NOTE: RequestQueue/ResponseQueue is not queue name but the name for
    //(1) keyed queue service of type IMessageQueue
    //(2) named queue options of type IOptionsMonitor<T>
    //(3) configuration section for the named queue options
    //Because of item (3), when the values are changed, the configuration file/environment variables/... must be changed accordingly.
    public const string RequestQueue = "Request";
    public const string ResponseQueue = "Response";

    //NOTE: The parameter "name" is the name of the config section under "Queues". It is also used as the name for Options<T>.
    public static IServiceCollection AddQueueOptions<T>(this IServiceCollection services, IConfiguration configuration, string name) where T : QueueOptions
    {
        var section = configuration.GetSection("Queues");
        services.AddOptions<T>(name)
            .Bind(section)
            .Configure(options =>
            {
                var opts = section.GetSection(name).Get<T>();
                options.Merge(opts);
            })
            .Validate(options =>
            {
                //TODO: To give user-friendly message of validation error, implement IValidateOptions
                return options.Validate();
            });
        return services;
    }

    //NOTE: The parameter "name" is used as: (1) keyed service name (2) Options<T> name (3) config section name
    public static IServiceCollection AddQueue(this IServiceCollection services, IConfiguration configuration, string name)
    {
        var queueType = configuration["Queues:QueueType"];

        if (string.IsNullOrEmpty(queueType) ||
            ServiceBusQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            services.AddKeyedTransient<IMessageQueue>(name, (provider, key) =>
            {
                var optionMonitor = provider.GetRequiredService<IOptionsMonitor<ServiceBusQueueOptions>>();
                var options = optionMonitor.Get(name);
                var logger = provider.GetRequiredService<ILogger<ServiceBusQueue>>();
                return new ServiceBusQueue(options, logger);
            });
            services.AddQueueOptions<ServiceBusQueueOptions>(configuration, name);
        }
        else if (StorageQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            services.AddKeyedTransient<IMessageQueue>(name, (provider, key) =>
            {
                var optionMonitor = provider.GetRequiredService<IOptionsMonitor<StorageQueueOptions>>();
                var options = optionMonitor.Get(name);
                return new StorageQueue(options);
            });
            services.AddQueueOptions<StorageQueueOptions>(configuration, name);
        }
        else
        {
            throw new ArgumentException($"Invalid queue type '{queueType}'!");
        }

        return services;
    }
}
