using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa
{
    public interface ISoaService
    {
        //TODO: Add a method for initialization
        //Task InitializeAsync(CancellationToken cancel = default);

        Task<string> InvokeAsync(string input, CancellationToken cancel = default);
    }
}
