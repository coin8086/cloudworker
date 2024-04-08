using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloudWorker.ServiceInterface;

public abstract class UserServiceBase : IUserService
{
    protected ILogger _logger;

    protected IConfiguration _hostConfig;

    public UserServiceBase(ILogger logger, IConfiguration hostConfig)
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
