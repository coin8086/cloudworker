using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa;

public class EchoService : IUserService
{
    public Task<string> InvokeAsync(string input, CancellationToken cancel = default)
    {
        return Task.FromResult(input);
    }
}
