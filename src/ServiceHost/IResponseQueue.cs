using System.Threading.Tasks;

namespace Cloud.Soa;

interface IResponseQueue
{
    Task SendAsync(string response);
}
