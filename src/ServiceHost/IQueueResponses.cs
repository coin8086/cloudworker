using System.Threading.Tasks;

namespace Cloud.Soa;

interface IQueueResponses
{
    Task SendAsync(string response);
}
