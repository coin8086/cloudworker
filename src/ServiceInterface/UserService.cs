using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker.ServiceInterface;

public abstract class UserService : IUserService
{
    protected ILogger _logger;

    protected IConfiguration _hostConfig;

    public UserService(ILogger logger, IConfiguration hostConfig)
    {
        _logger = logger;
        _hostConfig = hostConfig;
    }

    public virtual ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public virtual Task InitializeAsync(CancellationToken cancel = default)
    {
        return Task.CompletedTask;
    }

    public abstract Task<string> InvokeAsync(string input, CancellationToken cancel = default);
}
