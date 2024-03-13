namespace Cloud.Soa.Client;

public static class QueueClient
{
    public static IMessageQueue Create(QueueOptions options)
    {
        if (string.IsNullOrEmpty(options.QueueType) ||
            string.Equals(options.QueueType, ServiceBusQueue.QueueType, StringComparison.OrdinalIgnoreCase))
        {
            var queueOptions = new ServiceBusQueueOptions()
            {
                QueueType = options.QueueType,
                ConnectionString = options.ConnectionString,
                QueueName = options.QueueName,
                MessageLease = 60,
            };

            //TODO: Provide a console logger for ServiceBusQueue
            return new ServiceBusQueue(queueOptions);
        }
        else
        {
            var queueOptions = new StorageQueueOptions()
            {
                QueueType = options.QueueType,
                ConnectionString = options.ConnectionString,
                QueueName = options.QueueName,
                MessageLease = 60,
                QueryInterval = options.QueryInterval,
            };

            return new StorageQueue(queueOptions);
        }
    }
}
