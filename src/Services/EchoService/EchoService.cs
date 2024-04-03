using CloudWorker.ServiceInterface;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker.EchoService;

public class EchoService : IUserService
{
    //Neither ILogger nor IConfiguration is used in the service. So ignore them.
    public EchoService(object logger, object configuration) { }

    public Task InitializeAsync(CancellationToken cancel = default)
    {
        return Task.CompletedTask;
    }

    public Task<string> InvokeAsync(string input, CancellationToken cancel = default)
    {
        return Task.FromResult(input);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
