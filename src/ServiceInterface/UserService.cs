using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CloudWorker.ServiceInterface;

public abstract class UserService<TOptions> : UserServiceBase where TOptions : class
{
    protected TOptions? _options;

    protected virtual string _settingsFileName { get; } = "ServiceSettings.json";

    protected virtual string _environmentPrefix { get; } = "";

    public UserService(ILogger logger, IConfiguration hostConfig) : base(logger, hostConfig)
    {
        LoadConfiguration();
    }

    protected virtual void LoadConfiguration()
    {
        var configFileBase = Path.GetDirectoryName(this.GetType().Assembly.Location);
        if (string.IsNullOrWhiteSpace(configFileBase))
        {
            configFileBase = Directory.GetCurrentDirectory();
        }

        _logger.LogInformation("Look up configuration file in '{configFileBase}'.", configFileBase);

        var builder = new ConfigurationBuilder()
            .SetBasePath(configFileBase)
            .AddJsonFile(_settingsFileName, true)
            .AddEnvironmentVariables(_environmentPrefix);

        var configuration = builder.Build();
        _options = configuration.Get<TOptions>();
    }
}
