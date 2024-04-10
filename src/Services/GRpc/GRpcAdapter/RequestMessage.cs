using System.Text.Json;

namespace CloudWorker.GRpcAdapter;

public class RequestMessage
{
    public string? Id { get; set; }

    public string? ServiceName { get; set; }

    public string? MethodName { get; set; }

    public string? Payload { get; set; }

    public static RequestMessage FromJson(string value)
    {
        var msg = JsonSerializer.Deserialize<RequestMessage>(value);
        if (msg == null || msg?.Payload == null || string.IsNullOrEmpty(msg?.ServiceName) || string.IsNullOrEmpty(msg?.MethodName))
        {
            throw new ArgumentException($"Invalid value '{value}'!");
        }
        return msg;
    }
}
