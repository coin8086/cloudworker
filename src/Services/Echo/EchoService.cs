using CloudWorker.ServiceInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker.Services.Echo;

public class EchoService : UserServiceBase
{
    public EchoService(ILogger logger, IConfiguration hostConfig) : base(logger, hostConfig) {}

    public override Task<string> InvokeAsync(string input, CancellationToken cancel = default)
    {
        return Task.FromResult(input);
    }
}
