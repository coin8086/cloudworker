using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CloudWorker.Client.SDK.Bicep;

public class SecureEnvironmentVariable : IValidatable
{
    [Required]
    public string? Name { get; set; }

    public string? Value { get; set; }

    public string? SecureValue { get; set; }

    [MemberNotNull(nameof(Name))]
    public void Validate()
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
    {
        IValidatable.Validate(this);
        if (string.IsNullOrWhiteSpace(Value) && string.IsNullOrWhiteSpace(SecureValue))
        {
            throw new ArgumentException("Either 'Value' or 'SecureValue' should be provided.");
        }
    }
#pragma warning restore CS8774 // Member must have a non-null value when exiting.
}

public class FileShareMount : IValidatable
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public string? MountPath { get; set; }

    [Required]
    public string? FileShareName { get; set; }

    [Required]
    public string? StorageAccountName { get; set; }

    [Required]
    public string? storageAccountKey { get; set; }

    [MemberNotNull(nameof(Name), nameof(MountPath), nameof(FileShareName), nameof(StorageAccountName), nameof(storageAccountKey))]
    public void Validate()
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
    {
        IValidatable.Validate(this);
    }
#pragma warning restore CS8774 // Member must have a non-null value when exiting.
}

public class StarterParameters : IValidatable
{
    //TODO: Validate this property?
    public string? Location { get; set; }

    //TODO: Support custom service
    [Required]
    public string? Service {  get; set; }

    [ValidateElement]
    public IEnumerable<SecureEnvironmentVariable>? EnvironmentVariables { get; set; }

    [ValidateElement]
    public IEnumerable<FileShareMount>? FileShareMounts { get; set; }

    [Required]
    public string? MessagingRgName { get; set; }

    [Required]
    public string? ComputingRgName { get; set; }

    public string? ServiceBusName { get; set; }

    public string? AppInsightsName { get; set; }

    [MemberNotNull(nameof(Service), nameof(MessagingRgName), nameof(ComputingRgName))]
    public void Validate()
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
    {
        IValidatable.Validate(this);
    }
#pragma warning restore CS8774 // Member must have a non-null value when exiting.
}