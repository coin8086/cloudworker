using CloudWorker.GRpcAdapter;
using CloudWorker.GRpcAdapterClient;
using CloudWorker.ServiceInterface;
using GRpcHello;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace GRpcAdapterTest;

public class GRpcAdapaterTest : IDisposable
{
    private IUserService _service;

    public GRpcAdapaterTest(ITestOutputHelper output)
    {
        CheckEnvironmentVariables();

        var logger = new TestOutputLogger(output);
        var hostConfig = new ConfigurationBuilder().Build();

        _service = new GRpcAdapter(logger, hostConfig);
    }

    public void Dispose()
    {
        _service.DisposeAsync().AsTask().Wait();
    }

    private static void CheckEnvironmentVariables()
    {
        var list = new string[]
        {
            "GRPC_ServerURL",
            "GRPC_ServerFileName",
            "GRPC_ServerArguments"
        };
        foreach (var key in list)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(value)) {
                throw new InvalidOperationException($"Environment variable '{key}' is required but missing!");
            }
        }
    }

    [Fact]
    public async void EverythingIsOK()
    {
        await _service.InitializeAsync();
        Assert.True(true);

        //One gRPC call

        var gMethod = Greeter.Descriptor.FindMethodByName("SayHello");
        var gMsg = new HelloRequest() { Name = "Rob" };
        var request = new Request(gMethod, gMsg);

        var responseString = await _service.InvokeAsync(request.ToJson());
        Assert.False(string.IsNullOrWhiteSpace(responseString));

        var response = new Response<HelloReply>(responseString);
        Assert.Equal(request.Id, response.InReplyTo);
        Assert.Null(response.Error);
        Assert.NotNull(response.Payload);
        Assert.NotNull(response.GRpcMessage);
        Assert.Equal("Hello " + gMsg.Name, response.GRpcMessage.Message);

        //Many concurrent gRPC calls

        var num = 100;
        var tasks = new Task<string>[num];
        var reqString = request.ToJson();
        for (var i = 0; i < num; i++)
        {
            tasks[i] = _service.InvokeAsync(reqString);
        }
        await Task.WhenAll(tasks);
        Assert.DoesNotContain(tasks, t => t.IsFaulted);
        var responses = tasks.Select(t => new Response<HelloReply>(t.Result));
        var expectedMsg = "Hello " + gMsg.Name;
        Assert.DoesNotContain(responses,
            resp => resp.Error != null || resp.GRpcMessage == null || !resp.GRpcMessage.Message.Equals(expectedMsg));
    }

    [Fact]
    public async void GRpcServerIsDown()
    {
        //Simulate the condition that the server is down by no call to _service.InitializeAsync.

        var gMethod = Greeter.Descriptor.FindMethodByName("SayHello");
        var gMsg = new HelloRequest() { Name = "Rob" };
        var request = new Request(gMethod, gMsg);

        var responseString = await _service.InvokeAsync(request.ToJson());
        Assert.False(string.IsNullOrWhiteSpace(responseString));

        var response = new Response<HelloReply>(responseString);
        Assert.Equal(request.Id, response.InReplyTo);
        Assert.NotNull(response.Error);
        Assert.Null(response.Payload);
        Assert.Null(response.GRpcMessage);
    }
}