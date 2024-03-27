using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWork.CGIService;

public class CGIServiceOptions
{
    [Required]
    public required string FileName { get; set; }

    public string? Arguments { get; set; }
}

public class CGICallResult
{
    public int? ExitCode {  get; set; }

    public string? Stdout { get; set; }

    public string? Stderr { get; set; }

    public string? Exception {  get; set; }
}

public class CGIService : IUserService
{
    private readonly ILogger _logger;
    private readonly CGIServiceOptions _options;

    //NOTE: Users can leverage ILogger and IConfiguration from host, or build their own ones for
    //customization.
    public CGIService(ILogger logger, IConfiguration _)
    {
        _logger = logger;
        _options = LoadConfiguration();
    }

    private CGIServiceOptions LoadConfiguration()
    {
        var configFileBase = Path.GetDirectoryName(typeof(CGIService).Assembly.Location);
        if (string.IsNullOrWhiteSpace(configFileBase))
        {
            configFileBase = Directory.GetCurrentDirectory();
        }

        _logger.LogInformation("Look up configuration file in '{configFileBase}'.", configFileBase);

        var builder = new ConfigurationBuilder()
            .SetBasePath(configFileBase)
            .AddJsonFile("cgisettings.json", true)
            .AddEnvironmentVariables("CGI_");

        var configuration = builder.Build();
        var options = configuration.Get<CGIServiceOptions>();
        if (string.IsNullOrWhiteSpace(options?.FileName))
        {
            throw new ArgumentException("FileName is required but missing in configuration!");
        }

        _logger.LogInformation("FileName='{FileName}' Arguments='{Arguments}'", options.FileName, options.Arguments);
        return options;
    }

    public async Task<string> InvokeAsync(string input, CancellationToken cancel = default)
    {
        _logger.LogTrace("InvokeAsync: input={input}", input);

        //TODO: Set/modify/restrict environment variables?
        var startInfo = new ProcessStartInfo()
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            FileName = _options.FileName,
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "InvokeAsync: Error when running '{file}'", _options.FileName);
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
