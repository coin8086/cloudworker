using CloudWorker.GRpcAdapter;
using CloudWorker.MessageQueue;
using Google.Protobuf;

namespace CloudWorker.GRpcAdapterClient;

public class Response<T> : ResponseMessage where T : IMessage<T>, new()
{
    public IQueueMessage? QueueMessage { get; private set; }

    public T? GRpcMessage { get; private set; }

    public Response(IQueueMessage queueMessage) : this(queueMessage.Content)
    {
        QueueMessage = queueMessage;
    }

    public Response(string value)
    {
        var message = FromJson(value);
        InReplyTo = message.InReplyTo;
        Error = message.Error;
        Payload = message.Payload;
        if (Payload != null)
        {
            GRpcMessage = ParseGRpcMessageFrom(Payload);
        }
    }

    private static T ParseGRpcMessageFrom(string message)
    {
        var bytes = Convert.FromBase64String(message);
        var parser = new MessageParser<T>(() => new T());
        return (T)parser.ParseFrom(bytes!);
    }
}
