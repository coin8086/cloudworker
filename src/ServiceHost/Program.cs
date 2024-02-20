using Microsoft.Extensions.Hosting;

namespace Cloud.Soa;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var config = builder.Configuration;
        builder.Services.AddUserService(config);
        builder.Services.AddWorkerService(config);
        var host = builder.Build();
        host.Run();
    }
}
