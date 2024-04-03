using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker.ServiceInterface;

public abstract class UserService : IUserService
{
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
