using Azure.Core;
using CloudWorker.Client.SDK.Bicep;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudWorker.Client.SDK;

public enum ServiceType
{
    Custom,
    Echo,
    CGI,
    GRPC
}

public class AzureLocationJsonConverter : JsonConverter<AzureLocation>
{
    public override AzureLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return new AzureLocation(value!);
    }

    public override void Write(Utf8JsonWriter writer, AzureLocation value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

//TODO: More options in the config ...
public class ClusterConfig : IValidatable
{
    public Guid SubScriptionId { get; set; }

    //The location is for both ArmDeploymentContent.Location and an ARM template parameter
    [JsonConverter(typeof(AzureLocationJsonConverter))]
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
