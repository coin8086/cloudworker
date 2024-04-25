using GRpcHello.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;

namespace GRpcHello;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //NOTE: gRPC requires HTTP2 and for HTTP2 without TLS in ASP.NET, see
        //https://learn.microsoft.com/en-us/aspnet/core/grpc/aspnetcore?view=aspnetcore-8.0&tabs=visual-studio#protocol-negotiation
        //Also note the configuration like the following doesn't work for http2 without TLS (on Linux)!
        //"Kestrel": {
        //    "Endpoints": {
        //        "http": {
        //            "Url": "http://localhost:8080",
        //            "Protocols": "Http2"
        //        }
        //    }
        //}
        //So code configuration is made here.
        builder.WebHost.ConfigureKestrel((context, serverOptions) =>
        {
            serverOptions.Listen(IPAddress.Loopback, 8080, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });

        // Add services to the container.
        builder.Services.AddGrpc();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.MapGrpcService<GreeterService>();
        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        app.Run();
    }
}