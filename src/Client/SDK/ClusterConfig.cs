using CloudWorker.Client.SDK.Bicep;
using System.Collections.Generic;

namespace CloudWorker.Client.SDK;

public class ClusterConfig
{
    public string? SubScriptionId { get; set; }

    public string? Location { get; set; }

    public string? Service { get; set; }

    public ICollection<SecureEnvironmentVariable>? EnvironmentVariables { get; set; }

    public ICollection<FileShareMount>? FileShareMounts { get; set; }

    public void Valdiate()
    {
        throw new System.NotImplementedException();
    }
}
