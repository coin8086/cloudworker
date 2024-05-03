using System;

namespace CloudWorker.Client.SDK;

public class ClusterId
{
    public Guid SubscriptionId { get; }

    public Guid ResourceId { get; }

    public ClusterId(Guid subId, Guid resId)
    {
        SubscriptionId = subId;
        ResourceId = resId;
    }

    public ClusterId(string subId, Guid resId)
    {
        SubscriptionId = Guid.Parse(subId);
        ResourceId = resId;
    }

    public ClusterId(string subId, string resId)
    {
        SubscriptionId = Guid.Parse(subId);
        ResourceId = Guid.Parse(resId);
    }

    public override string ToString()
    {
        return $"{SubscriptionId}:{ResourceId}";
    }

    public static ClusterId FromString(string clusterId)
    {
        var components = clusterId.Split(':');
        if (components.Length != 2)
        {
            throw new ArgumentException($"Invalid cluster id '{clusterId}'!");
        }

        try
        {
            return new ClusterId(components[0], components[1]);
        }
        catch (FormatException)
        {
            throw new ArgumentException($"Invalid cluster id '{clusterId}'!");
        }
    }
}
