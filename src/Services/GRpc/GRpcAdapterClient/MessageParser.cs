using Google.Protobuf;

namespace CloudWorker.GRpcAdapterClient;

public class MessageParser<T> where T : IMessage<T>, new()
{
    private MessageParser _parser;

    public MessageParser()
    {
        _parser = new Google.Protobuf.MessageParser<T>(() => new T());
    }

    public T ParseFrom(string message)
    {
        //TODO: Is _parser thread safe?
        return ParseFrom(message, _parser);
    }

    public static T ParseFrom(string message, MessageParser? parser = null)
    {
        var bytes = Convert.FromBase64String(message);
        if (parser == null)
        {
            parser = new Google.Protobuf.MessageParser<T>(() => new T());
        }
        return (T)parser.ParseFrom(bytes!);
    }
}
