using System.Threading.Tasks;

namespace Cloud.Soa;

public class EchoService : IUserService
{
    public Task<string> InvokeAsync(string json)
    {
        return Task.FromResult(json);
    }
}
