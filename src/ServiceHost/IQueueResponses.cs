using System.Threading.Tasks;

namespace Cloud.Soa;

interface IQueueResponse
{
    string Message { get; set; }
}

interface IQueueResponses
{
    Task SendAsync(IQueueResponse response);

    Task SendAsync(string response);
}
