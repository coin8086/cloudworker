namespace Cloud.Soa;

public class QueueOptions
{
    public string? QueueType { get; set; }

    public string? QueueName { get; set; }

    public string? ConnectionString { get; set; }

    //Here no default value in code because some queue (like SBQ) can not set lease by code!
    //So this value MUST be provided by configuration!
    public int? MessageLease { get; set; } //In seconds

    public virtual void Merge(QueueOptions? other)
    {
        if (other == null)
        {
            return;
        }
        if (other.QueueType != null)
        {
            QueueType = other.QueueType;
        }
        if (other.QueueName != null)
        {
            QueueName = other.QueueName;
        }
        if (other.ConnectionString != null)
        {
            ConnectionString = other.ConnectionString;
        }
        if (other.MessageLease != null)
        {
            MessageLease = other.MessageLease;
        }
    }

    public virtual bool Validate()
    {
        return !string.IsNullOrWhiteSpace(QueueName) && !string.IsNullOrWhiteSpace(ConnectionString)
             && MessageLease > 0;
    }
}
