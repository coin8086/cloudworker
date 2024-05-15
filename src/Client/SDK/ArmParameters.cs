using CloudWorker.Client.SDK.ARM;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CloudWorker.Client.SDK;

public class ArmParamValue<T> where T : notnull
{
    public T Value { get; set; }

    public ArmParamValue(T value)
    {
        Value = value;
    }

    [return: NotNullIfNotNull(nameof(value))]
    static public ArmParamValue<T>? Create(T? value)
    {
        return value == null ? null : new ArmParamValue<T>(value);
    }

    [return: NotNullIfNotNull(nameof(value))]
    static public ArmParamValue<ST>? Create<ST>(Nullable<ST> value) where ST : struct
    {
        return value.HasValue ? new ArmParamValue<ST>(value.Value) : null;
    }
}

//TODO: Validate ArmParamValue<T> in IValidatable.Validate?
public class StarterParameters
{
    //TODO: Validate this property?
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ArmParamValue<string>? Location { get; set; }

    //TODO: Support custom service
    public required ArmParamValue<string> Service { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ArmParamValue<IEnumerable<SecureEnvironmentVariable>>? EnvironmentVariables { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ArmParamValue<IEnumerable<FileShareMount>>? FileShares { get; set; }

    public required ArmParamValue<string> MessagingRgName { get; set; }

    public required ArmParamValue<string> ComputingRgName { get; set; }

    public required ArmParamValue<string> ServiceBusName { get; set; }

    public required ArmParamValue<string> AppInsightsName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ArmParamValue<int>? NodeCount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ArmParamValue<ServiceBusQueueOptions>? ServiceBusQueueOptions { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ArmParamValue<NodeConfig>? NodeConfig { get; set; }
}