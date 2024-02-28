using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa
{
    public interface ISoaService
    {
        Task<string> InvokeAsync(string input, CancellationToken cancel = default);
    }
}
