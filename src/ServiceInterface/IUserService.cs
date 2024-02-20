using System.Threading.Tasks;

namespace Cloud.Soa
{
    public interface IUserService
    {
        Task<string> InvokeAsync(string json);
    }
}
