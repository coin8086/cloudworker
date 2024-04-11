using CloudWorker.GRpcAdapter;
using CloudWorker.GRpcAdapterClient;
using CloudWorker.ServiceInterface;
using GRpcHello;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GRpcAdapterTest;

public class GRpcAdapaterTest : IDisposable
{
    private ILoggerFactory _loggerFactory;

    private ILogger _logger;

    private IConfiguration _hostConfig;

    public GRpcAdapaterTest()
    {
        CheckEnvironmentVariables();

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders().AddSimpleConsole();
        });
        _logger = _loggerFactory.CreateLogger<GRpcAdapaterTest>();
        _hostConfig = new ConfigurationBuilder().Build();
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
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
    public async void IntegrationTest()
    {
        IUserService service = new GRpcAdapter(_logger, _hostConfig);
        Assert.True(true);

        await service.InitializeAsync();
        Assert.True(true);

        //Happy path
        var gMethod = Greeter.Descriptor.FindMethodByName("SayHello");
        var gMsg = new HelloRequest() { Name = "Rob" };
        var request = new Request(gMethod, gMsg);

        var responseString = await service.InvokeAsync(request.ToJson());
        Assert.False(string.IsNullOrWhiteSpace(responseString));

        var response = new Response<HelloReply>(responseString);
        Assert.NotNull(response.Message);
        Assert.Equal(request.Message.Id, response.Message.InReplyTo);
        Assert.Null(response.Message.Error);
        Assert.NotNull(response.Message.Payload);
        Assert.NotNull(response.GRpcMessage);
        Assert.Equal("Hello " + gMsg.Name, response.GRpcMessage.Message);

        //Bad path...

        await service.DisposeAsync();
        Assert.True(true);
    }
}