using CloudWorker.MessageQueue;
using Google.Protobuf;

namespace CloudWorker.Services.GRpc.Client;

public class Response<T> : ResponseMessage, IQueueMessage where T : IMessage<T>, new()
{
    private IQueueMessage? _queueMessage;

    public T? GRpcMessage { get; private set; }

    public string Id => _queueMessage?.Id ?? string.Empty;

    public string Content => _queueMessage?.Content ?? string.Empty;

    public Response(IQueueMessage queueMessage) : this(queueMessage.Content)
    {
        _queueMessage = queueMessage;
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

    public Task RenewLeaseAsync()
    {
        var qmsg = _queueMessage ?? throw new InvalidOperationException();
        return qmsg.RenewLeaseAsync();
    }

    public Task ReturnAsync()
    {
        var qmsg = _queueMessage ?? throw new InvalidOperationException();
        return qmsg.ReturnAsync();
    }

    public Task DeleteAsync()
    {
        var qmsg = _queueMessage ?? throw new InvalidOperationException();
        return qmsg.DeleteAsync();
    }

    private static T ParseGRpcMessageFrom(string message)
    {
        var bytes = Convert.FromBase64String(message);
        var parser = new MessageParser<T>(() => new T());
        return (T)parser.ParseFrom(bytes!);
    }
}
