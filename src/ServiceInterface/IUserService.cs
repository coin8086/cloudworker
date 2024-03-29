using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker
{
    public interface IUserService
    {
        //TODO: Add a method for initialization
        //Task InitializeAsync(CancellationToken cancel = default);

        Task<string> InvokeAsync(string input, CancellationToken cancel = default);
    }
}
