using CloudWorker.ServiceInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker.CGIService;

public class CGIServiceOptions
{
    public string? FileName { get; set; }

    public string? Arguments { get; set; }
}

public class CGICallResult
{
    public int? ExitCode {  get; set; }

    public string? Stdout { get; set; }

    public string? Stderr { get; set; }

    public string? Exception {  get; set; }
}

public class CGIService : UserService<CGIServiceOptions>
{
    protected override string _settingsFileName { get; } = "cgisettings.json";

    protected override string _environmentPrefix { get; } = "CGI_";

    public CGIService(ILogger logger, IConfiguration hostConfig) : base(logger, hostConfig) {}

    protected override void LoadConfiguration()
    {
        base.LoadConfiguration();
        if (string.IsNullOrWhiteSpace(_options?.FileName))
        {
            throw new ArgumentException("FileName is required but missing in configuration!");
        }
        _logger.LogInformation("FileName='{FileName}' Arguments='{Arguments}'", _options.FileName, _options.Arguments);
    }

    public override async Task<string> InvokeAsync(string input, CancellationToken cancel = default)
    {
        _logger.LogTrace("InvokeAsync: input={input}", input);

        //TODO: Set/modify/restrict environment variables?
        var startInfo = new ProcessStartInfo()
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            FileName = _options!.FileName,
            Arguments = _options.Arguments,
        };

        using var process = new Process()
        { 
            StartInfo = startInfo,

            //NOTE: This is to avoid the parent process being zombie in some situation. See 
            //https://github.com/dotnet/runtime/issues/21661
            EnableRaisingEvents = true,
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (sender, args) => {
            if (args.Data != null)
            {
                stdout.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (sender, args) => {
            if (args.Data != null)
            {
                stderr.AppendLine(args.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var stdin = process.StandardInput;
            await stdin.WriteAsync(input);
            stdin.Close();

            await process.WaitForExitAsync(cancel);

            var result = new CGICallResult()
            {
                ExitCode = process.ExitCode,
                Stdout = stdout.ToString(),
                Stderr = stderr.ToString()
            };
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            //TODO: rethrow when cancel.IsCancellationRequested && ex is OperationCanceledException
            //NOTE: consider a shrink down process, in which a running node is stopped. Then the request
            //being processed should be processed on a new node, instead of returning an error to client.
            if (cancel.IsCancellationRequested && ex is OperationCanceledException)
            {
                _logger.LogInformation("InvokeAsync: Operation is canceled.");
            }
            else
            {
                _logger.LogError(ex, "InvokeAsync: Error when running '{file}'", _options.FileName);
            }

            var result = new CGICallResult()
            {
                Stdout = stdout.ToString(),
                Stderr = stderr.ToString(),
                Exception = ex.ToString()
            };
            try
            {
                result.ExitCode = process.ExitCode;
            }
            catch { }
            return JsonSerializer.Serialize(result);
        }
    }
}
