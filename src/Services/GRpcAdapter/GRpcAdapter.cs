using CloudWorker.ServiceInterface;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloudWorker.GRpcAdapter;

public class GRpcAdapterOptions
{
    public string? ServerURL {  get; set; }
}

public class GRpcAdapter : UserService
{
    private readonly GRpcAdapterOptions _options;

    private GrpcChannel _channel;

    public GRpcAdapter(ILogger logger, IConfiguration hostConfig) : base(logger, hostConfig)
    {
        _options = LoadConfiguration(_logger);
        _channel = GrpcChannel.ForAddress(_options.ServerURL!);
    }

    private static GRpcAdapterOptions LoadConfiguration(ILogger logger)
    {
        throw new NotImplementedException();
    }

    public override Task InitializeAsync(CancellationToken cancel = default)
    {
        //TODO: Start gRPC server...
        throw new NotImplementedException();
    }

    public override Task<string> InvokeAsync(string input, CancellationToken cancel = default)
    {
        //TODO: Can an invoker be a class member?
        var invoker = _channel.CreateCallInvoker();
        //invoker.AsyncUnaryCall()
        throw new NotImplementedException();
    }
}
