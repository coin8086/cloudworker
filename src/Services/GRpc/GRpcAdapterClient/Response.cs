using CloudWorker.GRpcAdapter;
using CloudWorker.MessageQueue;
using Google.Protobuf;

namespace CloudWorker.GRpcAdapterClient;

public class Response<T> where T : IMessage<T>, new()
{
    public IQueueMessage QueueMessage { get; private set; }

    public ResponseMessage Message { get; private set; }

    public T GRpcMessage { get; private set; }

    public Response(IQueueMessage queueMessage)
    {
        QueueMessage = queueMessage;
        Message = ResponseMessage.FromJson(QueueMessage.Content);
        GRpcMessage = ParseGRpcMessageFrom(Message.Payload!);
    }

    public static T ParseGRpcMessageFrom(string message)
    {
        var bytes = Convert.FromBase64String(message);
        var parser = new Google.Protobuf.MessageParser<T>(() => new T());
        return (T)parser.ParseFrom(bytes!);
    }
}
