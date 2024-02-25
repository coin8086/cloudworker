using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace Cloud.Soa;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();
        var config = builder.Configuration;

        var switchMappings = new Dictionary<string, string>()
        {
            { "-p", "UserService:AssemblyPath" },
        };

        config.AddCommandLine(args, switchMappings);

        builder.Services.AddUserService(config);
        builder.Services.AddRequestQueue(config);
        builder.Services.AddResponseQueue(config);
        builder.Services.AddWorkerService(config);
        var host = builder.Build();
        host.Run();
    }
}
