using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker
{
    public interface IUserService : IAsyncDisposable
    {
        Task InitializeAsync(CancellationToken cancel = default);

        Task<string> InvokeAsync(string input, CancellationToken cancel = default);
    }
}
