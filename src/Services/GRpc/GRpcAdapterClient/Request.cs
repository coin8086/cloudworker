using Google.Protobuf.Reflection;
using Google.Protobuf;
using CloudWorker.GRpcAdapter;

namespace CloudWorker.GRpcAdapterClient;

public class Request : RequestMessage
{
    public Request(MethodDescriptor method, IMessage message, string? requestId = null)
    {
        if (method.IsClientStreaming || method.IsServerStreaming)
        {
            throw new InvalidOperationException("gRPC streaming call is not supported!");
        }
        Id = requestId ?? Guid.NewGuid().ToString();
        ServiceName = method.Service.FullName;
        MethodName = method.Name;
        Payload = Convert.ToBase64String(message.ToByteArray());
    }
}
