using System;

namespace CloudWorker.Client.SDK.Bicep;

public enum ServiceType
{
    Custom,
    Echo,
    CGI,
    GRPC
}

public class SecureEnvironmentVariable
{
    public string? Name { get; set; }

    public string? Value { get; set; }

    public string? SecureValue { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Cannot be empty.", nameof(Name));
        }
        if (string.IsNullOrWhiteSpace(Value) && string.IsNullOrWhiteSpace(SecureValue))
        {
            throw new ArgumentException("Either 'Value' or 'SecureValue' should be provided.");
        }
    }
}

public class FileShareMount
{
    public string? Name { get; set; }

    public string? MountPath { get; set; }

    public string? FileShareName { get; set; }

    public string? StorageAccountName { get; set; }

    public string? storageAccountKey { get; set; }

    public void Validate()
    {
        var properties = typeof(FileShareMount).GetProperties();
        foreach (var property in properties) {
            var value = (string?)property.GetValue(this);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Cannot be empty.", property.Name);
            }
        }
    }
}
