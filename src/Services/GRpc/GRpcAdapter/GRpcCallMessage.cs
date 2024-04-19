using System;

namespace CloudWorker.Services.GRpc;

class GRpcCallMessage
{
    public byte[] Content { get; set; }

    public GRpcCallMessage(byte[] input)
    {
        Content = input;
    }

    public string ToBase64()
    {
        return Convert.ToBase64String(Content);
    }

    public static GRpcCallMessage FromBase64(string value)
    {
        var bytes = Convert.FromBase64String(value);
        return new GRpcCallMessage(bytes);
    }

    public static byte[] Serialize(GRpcCallMessage msg)
    {
        return msg.Content;
    }

    public static GRpcCallMessage Deserialize(byte[] bytes)
    {
        return new GRpcCallMessage(bytes);
    }
}
