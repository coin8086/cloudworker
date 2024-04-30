using CloudWorker.Client.SDK.Bicep;
using System.Collections.Generic;

namespace CloudWorker.Client.SDK;

public class ClusterConfig : IValidatable
{
    [Required]
    public string? SubScriptionId { get; set; }

    [Required]
    public string? Location { get; set; }

    [Required]
    public string? Service { get; set; }

    public ICollection<SecureEnvironmentVariable>? EnvironmentVariables { get; set; }

    public ICollection<FileShareMount>? FileShareMounts { get; set; }

    public void Validate()
    {
        IValidatable.Validate(this);
        if (EnvironmentVariables != null)
        {
            foreach (var envVar in  EnvironmentVariables)
            {
                envVar.Validate();
            }
        }
        if (FileShareMounts != null)
        {
            foreach (var mount in FileShareMounts)
            {
                mount.Validate();
            }
        }
    }
}
