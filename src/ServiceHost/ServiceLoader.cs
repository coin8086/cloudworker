using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;

namespace Cloud.Soa;

interface IServiceLoader
{
    ISoaService CreateServiceInstance();
}

class ServiceLoaderOptions
{
    [Required]
    public string? AssemblyPath { get; set; }
}

class ServiceLoader : IServiceLoader
{
    private readonly ILogger _logger;
    private readonly ILogger _userLogger;
    private readonly IConfiguration _configuration;
    private readonly ServiceLoaderOptions _options;

    public ServiceLoader(ILogger<ServiceLoader> logger, ILogger<ISoaService> userLogger,
        IConfiguration configuration, IOptions<ServiceLoaderOptions> options)
    {
        _logger = logger;
        _userLogger = userLogger;
        _configuration = configuration;
        _options = options.Value;
    }

    public ISoaService CreateServiceInstance()
    {
        try
        {
            var assembly = LoadAssembly(_options.AssemblyPath!);
            var type = GetUserServiceType(assembly);
            var instance = (Activator.CreateInstance(type, _userLogger, _configuration) as ISoaService)!;
            return instance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when creating user service instance");
            throw;
        }
    }

    static Assembly LoadAssembly(string path)
    {
        var loadContext = new ServiceAssemblyLoadContext(path);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
    }

    static Type GetUserServiceType(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (typeof(ISoaService).IsAssignableFrom(type))
            {
                return type;
            }
        }

        throw new ApplicationException($"Can't find a type that implements ISoaService in {assembly} from {assembly.Location}.");
    }
}

static class ServiceCollectionUserServiceExtensions
{
    public static IServiceCollection AddUserService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IServiceLoader, ServiceLoader>();

        services.AddOptionsWithValidateOnStart<ServiceLoaderOptions>()
            .Bind(configuration.GetSection("Service"))
            .ValidateDataAnnotations();

        services.AddTransient<ISoaService>(provider => provider.GetService<IServiceLoader>()!.CreateServiceInstance());
        return services;
    }
}
