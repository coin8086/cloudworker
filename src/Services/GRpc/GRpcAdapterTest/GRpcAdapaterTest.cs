using CloudWorker.GRpcAdapter;
using CloudWorker.GRpcAdapterClient;
using CloudWorker.ServiceInterface;
using GRpcHello;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GRpcAdapterTest;

public class GRpcAdapaterTest
{
    private (ILogger logger, IConfiguration hostConfig) PrepareHostingEnvrionment(GRpcAdapterOptions opts)
    {
        throw new NotImplementedException();
    }

    [Fact]
    public async void Test1()
    {
        //TODO: Provide real options...
        var (logger, hostConfig) = PrepareHostingEnvrionment(new GRpcAdapterOptions());
        IUserService service = new GRpcAdapter(logger, hostConfig);
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