using System.Diagnostics.CodeAnalysis;

namespace CloudWorker.Client.SDK;

public interface IServiceProperties
{
    public string Service { get; }
}

public interface IQueueProperties
{
    public string QueueType { get; }

    public string ConnectionString { get; }

    public string RequestQueueName { get; }

    public string ResponseQueueName { get; }
}

public interface IClusterProperties
{
    public IServiceProperties ServiceProperties { get; }

    public IQueueProperties QueueProperties { get; }
}

#pragma warning disable CS8774 // Member must have a non-null value when exiting.
#pragma warning disable CS8766 // Nullability of reference types in return type of doesn't match implicitly implemented member (possibly because of nullability attributes).

public class ServiceProperties : IServiceProperties, IValidatable
{
    [Required]
    public string? Service {  get; set; }

    [MemberNotNull(nameof(Service))]
    public void Validate()
    {
        IValidatable.Validate(this);
    }
}

public class QueueProperties : IQueueProperties, IValidatable
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

public class ClusterProperties : IClusterProperties, IValidatable
{
    [Required]
    [ValidateObject]
    public IServiceProperties? ServiceProperties { get; set; }

    [Required]
    [ValidateObject]
    public IQueueProperties? QueueProperties { get; set; }

    [MemberNotNull(nameof(ServiceProperties), nameof(QueueProperties))]
    public void Validate()
    {
        IValidatable.Validate(this);
    }
}

#pragma warning restore CS8774 // Member must have a non-null value when exiting.
#pragma warning restore CS8766 // Nullability of reference types in return type of doesn't match implicitly implemented member (possibly because of nullability attributes).
