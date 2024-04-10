using CloudWorker.GRpcAdapter;
using CloudWorker.ServiceInterface;
using Google.Protobuf;
using GRpcHello;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
        var gRequest = new HelloRequest() { Name = "Rob" };
        var request = new RequestMessage()
        {
            //TODO: Provide full ServiceName...
            Id = "1", ServiceName = "GreeterService", MethodName = "SayHello",
            Payload = Convert.ToBase64String(gRequest.ToByteArray())
        };
        var requestString = JsonSerializer.Serialize(request);

        var responseString = await service.InvokeAsync(requestString);
        Assert.NotEmpty(responseString);

        var response = JsonSerializer.Deserialize<ResponseMessage>(responseString);
        Assert.NotNull(response);
        Assert.NotNull(response.Payload);

        var parser = new MessageParser<HelloReply>(() => new HelloReply());
        var gResponse = parser.ParseFrom(Convert.FromBase64String(response.Payload));
        Assert.NotNull(gResponse);

        //Sad path...

        await service.DisposeAsync();
        Assert.True(true);
    }
}