using CloudWorker.ServiceInterface;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker.Services.GRpc;

public class GRpcAdapterOptions
{
    public string? ServerURL {  get; set; }

    public string? ServerFileName { get; set;}

    public string? ServerArguments { get; set; }

    public int? ServerUpTimeout { get; set; } = 30;

    public static GRpcAdapterOptions Default = new GRpcAdapterOptions();
}

public class GRpcAdapter : UserService<GRpcAdapterOptions>
{
    protected override string _settingsFileName { get; } = "grpcsettings.json";

    protected override string _environmentPrefix { get; } = "GRPC_";

    private GrpcChannel _channel;

    private Process? _serverProcess;

    public GRpcAdapter(ILogger logger, IConfiguration hostConfig) : base(logger, hostConfig)
    {
        _channel = GrpcChannel.ForAddress(_options!.ServerURL!);
    }

    protected override void LoadConfiguration()
    {
        base.LoadConfiguration();

        if (_options == null)
        {
            throw new ArgumentException("No configuration for GRpc!");
        }
        if (string.IsNullOrWhiteSpace(_options.ServerURL) || string.IsNullOrEmpty(_options.ServerFileName))
        {
            throw new ArgumentException("ServerURL or ServerFileName is empty!");
        }
        _options.ServerUpTimeout ??= GRpcAdapterOptions.Default.ServerUpTimeout;

        _logger.LogInformation("gRPC server URL: {url}", _options.ServerURL);
    }

    public override async Task InitializeAsync(CancellationToken cancel = default)
    {
        var startInfo = new ProcessStartInfo()
        {
            UseShellExecute = false,
            FileName = _options!.ServerFileName,
            Arguments = _options.ServerArguments,
        };

        _serverProcess = new Process()
        {
            StartInfo = startInfo,

            //NOTE: This is to avoid the parent process being zombie in some situation. See
            //https://github.com/dotnet/runtime/issues/21661
            EnableRaisingEvents = true,
        };

        try
        {
            //TODO: Shall we monitor the server process and reinitialize one or throw a fatal exception when it's down?
            _serverProcess.Start();
            _logger.LogInformation("gRPC server process id: {id}", _serverProcess.Id);

            await ServerUpOrThrow();
            _logger.LogInformation("gRPC server is up.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when starting server process with '{filename}'.", _options.ServerFileName);
            throw;
        }
    }

    private async Task ServerUpOrThrow()
    {
        using var source = new CancellationTokenSource();
        var token = source.Token;
        var up = false;

        var checkTask = Task.Run(async () =>
        {
            await _channel.ConnectAsync(token);
            up = true;
        });

        var timerTask = Task.Run(async () =>
        {
            await Task.Delay((int)(_options!.ServerUpTimeout! * 1000), token);
            source.Cancel();
        });

        await Task.WhenAny(checkTask, timerTask);
        source.Cancel();
        if (!up)
        {
            throw new ApplicationException($"gRPC server doesn't come up within {_options!.ServerUpTimeout!} seconds!");
        }
    }

    public override ValueTask DisposeAsync()
    {
        try
        {
            //NOTE: Dispose() doesn't ensure to end the process so we kill it.
            _logger.LogInformation("Kill gRPC server process {id}", _serverProcess?.Id);
            _serverProcess?.Kill();
        }
        catch (Exception) {}

        _serverProcess?.Dispose();
        _serverProcess = null;
        return base.DisposeAsync();
    }

    public override async Task<string> InvokeAsync(string input, CancellationToken cancel = default)
    {
        RequestMessage? requestMsg = null;
        ResponseMessage? responseMsg = null;
        try
        {
            requestMsg = RequestMessage.FromJson(input);
            var request = GRpcCallMessage.FromBase64(requestMsg.Payload!);
            var method = new Method<GRpcCallMessage, GRpcCallMessage>(
                MethodType.Unary, requestMsg.ServiceName!, requestMsg.MethodName!,
                Marshallers.Create(GRpcCallMessage.Serialize, GRpcCallMessage.Deserialize),
                Marshallers.Create(GRpcCallMessage.Serialize, GRpcCallMessage.Deserialize));

            //TODO: Can an invoker be a class member?
            var invoker = _channel.CreateCallInvoker();

            var callOptions = new CallOptions(cancellationToken: cancel);
            var result = await invoker.AsyncUnaryCall(method, null, callOptions, request);
            responseMsg = new ResponseMessage() { InReplyTo = requestMsg.Id, Payload = result.ToBase64() };
        }
        catch (Exception ex)
        {
            //TODO: rethrow when cancel.IsCancellationRequested && ex is OperationCanceledException
            if (cancel.IsCancellationRequested && ex is OperationCanceledException)
            {
                _logger.LogInformation("Operation is canceled.");
            }
            else
            {
                _logger.LogError(ex, "Error when invoking gRPC method '{service}.{method}'.", requestMsg?.ServiceName, requestMsg?.MethodName);
            }
            responseMsg = new ResponseMessage() { InReplyTo = requestMsg?.Id, Error = ex.ToString() };
        }
        return responseMsg.ToJson();
    }
}
