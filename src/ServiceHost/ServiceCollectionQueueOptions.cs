﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cloud.Soa;

static class ServiceCollectionQueueOptions
{
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
}
