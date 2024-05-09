using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CloudWorker.Client.SDK.Bicep;

#pragma warning disable CS8774 // Member must have a non-null value when exiting.

public class SecureEnvironmentVariable : IValidatable
{
    [Required]
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SecureValue { get; set; }

    [MemberNotNull(nameof(Name))]
    public void Validate()
    {
        IValidatable.Validate(this);
        if (string.IsNullOrWhiteSpace(Value) && string.IsNullOrWhiteSpace(SecureValue))
        {
            throw new ArgumentException("Either 'Value' or 'SecureValue' should be provided.");
        }
    }
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
    {
        IValidatable.Validate(this);
    }
}

#pragma warning restore CS8774 // Member must have a non-null value when exiting.
