using Google.Protobuf.Reflection;
using Google.Protobuf;
using CloudWorker.GRpcAdapter;

namespace CloudWorker.GRpcAdapterClient;

public class Request
{
    public RequestMessage Message { get; private set; }

    public Request(MethodDescriptor method, IMessage message, string? requestId = null)
    {
        Message = BuildRequestMessage(method, message, requestId);
    }

    public string ToJson()
    {
        return Message.ToJson();
    }

    public static RequestMessage BuildRequestMessage(MethodDescriptor method, IMessage message, string? requestId = null)
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
