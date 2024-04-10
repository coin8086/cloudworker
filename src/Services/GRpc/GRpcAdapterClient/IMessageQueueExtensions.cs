using CloudWorker.MessageQueue;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace CloudWorker.GRpcAdapterClient;

public static class IMessageQueueExtensions
{
    public static Task SendGRpcMessageAsync(this IMessageQueue queue, MethodDescriptor method,
        IMessage message, string? requestId = null, CancellationToken cancel = default)
    {
        var request = RequestBuilder.Build(method, message, requestId);
        return queue.SendAsync(request.ToJson(), cancel);
    }
}
