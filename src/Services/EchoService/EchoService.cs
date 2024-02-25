using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa;

public class EchoService : IUserService
{
    public EchoService(object _) {}

    public Task<string> InvokeAsync(string input, CancellationToken cancel = default)
    {
        return Task.FromResult(input);
    }
}
