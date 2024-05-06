using CloudWorker.Client.SDK.Bicep;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CloudWorker.Client.SDK;

public enum ServiceType
{
    Custom,
    Echo,
    CGI,
    GRPC
}

//TODO: More options in the config ...
public class ClusterConfig : IValidatable
{
    [Required]
    public string? SubScriptionId { get; set; }

    public string? Location { get; set; }

    public ServiceType Service { get; set; }

    [ValidateElement]
    public IEnumerable<SecureEnvironmentVariable>? EnvironmentVariables { get; set; }

    [ValidateElement]
    public IEnumerable<FileShareMount>? FileShareMounts { get; set; }

    [MemberNotNull(nameof(SubScriptionId), nameof(Location))]
    public void Validate()
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
    {
        IValidatable.Validate(this);
        //TODO: validate Guid of SubScriptionId and custom type of service 
    }
#pragma warning restore CS8774 // Member must have a non-null value when exiting.
}
