using System;

namespace CloudWorker.Client.SDK.Bicep;

public enum ServiceType
{
    Custom,
    Echo,
    CGI,
    GRPC
}

public class SecureEnvironmentVariable : IValidatable
{
    [Required]
    public string? Name { get; set; }

    public string? Value { get; set; }

    public string? SecureValue { get; set; }

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

    public void Validate()
    {
        IValidatable.Validate(this);
    }
}
