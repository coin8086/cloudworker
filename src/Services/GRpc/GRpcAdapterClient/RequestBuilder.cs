using CloudWorker.GRpcAdapter;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace CloudWorker.GRpcAdapterClient;

public static class RequestBuilder
{
    public static RequestMessage Build(MethodDescriptor method, IMessage message, string? requestId = null)
    {
        var reqestMessage = new RequestMessage()
        {
            Id = requestId,
            ServiceName = method.Service.FullName,
            MethodName = method.Name,
            Payload = Convert.ToBase64String(message.ToByteArray())
        };
        return reqestMessage;
    }
}
