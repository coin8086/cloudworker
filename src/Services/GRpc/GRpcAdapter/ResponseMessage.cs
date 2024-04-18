using System;
using System.Text.Json;

namespace CloudWorker.Services.GRpcAdapter;

public class ResponseMessage
{
    public string? InReplyTo { get; set; }

    public string? Error { get; set; }

    public string? Payload { get; set; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    public static ResponseMessage FromJson(string value)
    {
        var msg = JsonSerializer.Deserialize<ResponseMessage>(value);
        if (msg == null)
        {
            throw new ArgumentException($"Invalid value '{value}'!");
        }
        return msg;
    }
}
