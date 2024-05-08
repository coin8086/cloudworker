using Azure.Core;
using CloudWorker.Client.SDK.Bicep;
using System;
using System.Collections.Generic;

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
    public Guid SubScriptionId { get; set; }

    //The location is for both ArmDeploymentContent.Location and an ARM template parameter
    public AzureLocation Location { get; set; } = AzureLocation.SoutheastAsia;

    public ServiceType Service { get; set; } = ServiceType.Echo;

    [ValidateElement]
    public IEnumerable<SecureEnvironmentVariable>? EnvironmentVariables { get; set; }

    [ValidateElement]
    public IEnumerable<FileShareMount>? FileShareMounts { get; set; }

    public void Validate()
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
    {
        IValidatable.Validate(this);
        //TODO: validate Guid of SubScriptionId and custom type of service 
    }
#pragma warning restore CS8774 // Member must have a non-null value when exiting.
}
