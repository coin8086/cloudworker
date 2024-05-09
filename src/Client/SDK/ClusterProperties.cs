using System.Diagnostics.CodeAnalysis;

namespace CloudWorker.Client.SDK;

#pragma warning disable CS8774 // Member must have a non-null value when exiting.

public class ServiceProperties : IValidatable
{
    [Required]
    public string? Service {  get; set; }

    [MemberNotNull(nameof(Service))]
    public void Validate()
    {
        IValidatable.Validate(this);
    }
}

public class QueueProperties : IValidatable
{
    [Required]
    public string? QueueType { get; set; }

    [Required]
    public string? ConnectionString { get; set; }

    [Required]
    public string? RequestQueueName { get; set; }

    [Required]
    public string? ResponseQueueName { get; set; }

    [MemberNotNull(nameof(QueueType), nameof(ConnectionString), nameof(RequestQueueName), nameof(ResponseQueueName))]
    public void Validate()
    {
        IValidatable.Validate(this);
    }
}

public class ClusterProperties : IValidatable
{
    [Required]
    [ValidateObject]
    public ServiceProperties? ServiceProperties { get; set; }

    [Required]
    [ValidateObject]
    public QueueProperties? QueueProperties { get; set; }

    [MemberNotNull(nameof(ServiceProperties), nameof(QueueProperties))]
    public void Validate()
    {
        IValidatable.Validate(this);
    }
}

#pragma warning restore CS8774 // Member must have a non-null value when exiting.
