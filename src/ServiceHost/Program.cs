using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cloud.Soa.ServiceHost;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();
        var config = builder.Configuration;
        config.AddCommandLine(args);

        builder.Services.AddApplicationInsights(config);
        builder.Services.AddUserService(config);
        builder.Services.AddQueue(config, Queues.RequestQueue);
        builder.Services.AddQueue(config, Queues.ResponseQueue);
        builder.Services.AddWorkerService(config);
        var host = builder.Build();
        host.Run();
    }
}
