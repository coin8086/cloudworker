using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa
{
    public interface IUserService
    {
        Task<string> InvokeAsync(string input, CancellationToken cancel = default);
    }
}
