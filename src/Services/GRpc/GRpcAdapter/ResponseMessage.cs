using System.Text.Json;

namespace CloudWorker.GRpcAdapter;

public class ResponseMessage
{
    public string? InReplyTo { get; set; }

    public string? Error { get; set; }

    public string? Payload { get; set; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}
