using Azure.Core;
using Azure.Identity;
using CloudWorker.Client.SDK;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace SessionSample;

class Program
{
    enum Action
    {
        Create,
        Update,
        Use,
        Delete
    }

    static void ShowUsageAndExit(int exitCode = 0)
    {
        var usage = @"
Usage:
{0} {{--create --config <session config file> | --update <session id> --config <session config file> | --use <clusetr id> | --delete <session id>}} [--debug] [--help | -h]
";
        Console.WriteLine(string.Format(usage, typeof(Program).Assembly.GetName().Name));
        Environment.Exit(exitCode);
    }

    static (Action action, string? id, string? configFile, bool debug) ParseCommandLine(string[] args)
    {
        Action? action = null;
        string? id = null;
        string? configFile = null;
        bool debug = false;

        try
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--create":
                        action = Action.Create;
                        break;
                    case "--update":
                        action = Action.Update;
                        id = args[++i];
                        break;
                    case "--use":
                        action = Action.Use;
                        id = args[++i];
                        break;
                    case "--delete":
                        action = Action.Delete;
                        id = args[++i];
                        break;
                    case "--config":
                        configFile = args[++i];
                        break;
                    case "--debug":
                        debug = true;
                        break;
                    case "-h":
                    case "--help":
                        ShowUsageAndExit(0);
                        break;
                    default:
                        throw new ArgumentException("Unkown argument!", args[i]);
                }
            }
            if (action == null)
            {
                throw new ArgumentException("Action must be specified.");
            }
            if (action != Action.Create && id == null)
            {
                throw new ArgumentException($"Session ID is requried for action {action}.");
            }
            if ((action == Action.Create || action == Action.Update))
            {
                if (string.IsNullOrWhiteSpace(configFile))
                {
                    throw new ArgumentException("Session configuration file must be specified.", "--config");
                }
                if (!File.Exists(configFile))
                {
                    throw new ArgumentException("Session configuration file doesn't exist.", "--config");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ShowUsageAndExit(1);
        }
        Debug.Assert(action != null);
        return (action.Value, id, configFile, debug);
    }

    static void CreateSession(string configFile)
    {
        var config = GetConfig(configFile);
        var session = Session.CreateOrUpdateAsync(Credential, config, null, LoggerFactory).Result;
    }

    static void UpdateSession(string id, string configFile)
    {
        var config = GetConfig(configFile);
        var session = Session.CreateOrUpdateAsync(Credential, config, id, LoggerFactory).Result;
    }

    static void UseSession(string id)
    {
        var session = Session.GetAsync(Credential, id, LoggerFactory).Result;
    }

    static void DeleteSession(string id)
    {
        Session.DestroyAsync(Credential, id, LoggerFactory).Wait();
    }

    static SessionConfig GetConfig(string configFile)
    {
        var content = File.ReadAllText(configFile);
        var config = JsonSerializer.Deserialize<SessionConfig>(content);
        if (config == null)
        {
            throw new InvalidDataException("Invalid configuration!");
        }
        config.Validate();
        return config;
    }

    static bool DebugOut { get; set; } = false;

    static Lazy<ILoggerFactory> _lazyLoggerFactory = new Lazy<ILoggerFactory>(() =>
    {
        return Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
            });
            if (DebugOut)
            {
                builder.SetMinimumLevel(LogLevel.Trace);
            }
        });
    });

    static ILoggerFactory LoggerFactory => _lazyLoggerFactory.Value;

    static TokenCredential Credential => new DefaultAzureCredential();

    static void Main(string[] args)
    {
        var (action, id, configFile, debug) = ParseCommandLine(args);
        DebugOut = debug;
        switch (action)
        {
            case Action.Create:
                CreateSession(configFile!);
                break;
            case Action.Update:
                UpdateSession(id!, configFile!);
                break;
            case Action.Delete:
                DeleteSession(id!);
                break;
            case Action.Use:
                UseSession(id!);
                break;
        }
    }
}
