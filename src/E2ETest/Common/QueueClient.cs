namespace Cloud.Soa.Client;

public static class QueueClient
{
    public static IMessageQueue Create(QueueOptions options)
    {
        var queueOptions = new Soa.StorageQueueOptions()
        {
            QueueType = options.QueueType,
            ConnectionString = options.ConnectionString,
            QueueName = options.QueueName,
            MessageLease = 60,
        };

        if (options is StorageQueueOptions responseQueueOpts)
        {
            queueOptions.QueryInterval = responseQueueOpts.QueryInterval;
        }

        if (string.IsNullOrEmpty(options.QueueType) ||
            string.Equals(options.QueueType, ServiceBusQueue.QueueType, StringComparison.OrdinalIgnoreCase))
        {
            return new ServiceBusQueue(queueOptions);
        }
        else
        {
            return new StorageQueue(queueOptions);
        }
    }
}
