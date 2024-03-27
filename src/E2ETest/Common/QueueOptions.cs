using CloudWork.MessageQueue;
using CommandLine;

namespace CloudWork.E2E;

public class QueueOptions
{
    public QueueOptions() {}

    public QueueOptions(QueueOptions options)
    {
        ConnectionString = options.ConnectionString;
        QueueType = options.QueueType;
        QueueName = options.QueueName;
    }

    const string ENV_CONNECTION_STRING = "QUEUE_CONNECTION_STRING";

    [Option('C', "connection-string", HelpText = $"Can also be set by env var '{ENV_CONNECTION_STRING}'")]
    public virtual string? ConnectionString { get; set; }

    [Option('t', "queue-type", HelpText = $"Can be '{ServiceBusQueue.QueueType}' or '{StorageQueue.QueueType}'.", Default = (string)ServiceBusQueue.QueueType)]
    public virtual string? QueueType { get; set; }

    [Option('n', "queue-name", Required = true)]
    public virtual string? QueueName { get; set; }

    [Option('i', "query-interval", Default = (int)500, HelpText = "For storage queue only")]
    public virtual int QueryInterval { get; set; }

    public virtual void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            var connectionString = Environment.GetEnvironmentVariable(ENV_CONNECTION_STRING);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"Connection string is missing! Set it either in command line or in environment variable {ENV_CONNECTION_STRING}!");
            }
            ConnectionString = connectionString;
        }
        if (!string.IsNullOrEmpty(QueueType))
        {
            if (!QueueType.Equals(StorageQueue.QueueType, StringComparison.OrdinalIgnoreCase) &&
                !QueueType.Equals(ServiceBusQueue.QueueType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid queue type '{QueueType}'!");
            }
        }
    }
}
