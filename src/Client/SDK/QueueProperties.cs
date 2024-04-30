namespace CloudWorker.Client.SDK;

public class QueueProperties
{
    public string? QueueType { get; set; }

    public string? ConnectionString {  get; set; }

    public string? RequestQueueName { get; set; }

    public string? ResponseQueueName { get; set; }
}
