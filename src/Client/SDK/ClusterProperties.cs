namespace CloudWorker.Client.SDK;

public class ServiceProperties
{
    public string? Service {  get; set; }
}

public class QueueProperties
{
    public string? QueueType { get; set; }

    public string? ConnectionString { get; set; }

    public string? RequestQueueName { get; set; }

    public string? ResponseQueueName { get; set; }
}

public class ClusterProperties
{
    public ServiceProperties? ServiceProperties { get; set; }

    public QueueProperties? QueueProperties { get; set; }

}
