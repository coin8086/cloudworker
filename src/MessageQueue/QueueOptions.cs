using System.ComponentModel.DataAnnotations;

namespace Cloud.Soa;

public class QueueOptions
{
    public string? QueueType { get; set; }

    [Required]
    public string? QueueName { get; set; }

    [Required]
    public string? ConnectionString { get; set; }

    [Required]
    //Here no default value in code because some queue (like SBQ) can not set lease by code!
    //So this value MUST be provided by configuration!
    public int? MessageLease { get; set; } //In seconds

    public int? QueryInterval { get; set; } = 200;  //In milliseconds.

    public static QueueOptions Default { get; } = new QueueOptions();
}
