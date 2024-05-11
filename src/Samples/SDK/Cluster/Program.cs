using Azure.Core;
using Azure.Identity;
using CloudWorker.Client.SDK;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClusterSample;

class Program
{
    enum Action
    {
        Create,
        Update,
        Use,
        Delete
    }

    class Options
    {
        public Action? Action { get; set; }

        public string? Id { get; set; }

        public string? ConfigFile { get; set; }

        public bool Debug { get; set; } = false;

        public void Validate()
        {
            if (Action == null)
            {
                throw new ArgumentException("Action must be specified.");
            }
            if (Action != Program.Action.Create && Id == null)
            {
                throw new ArgumentException($"Id is requried for action {Action}.");
            }
            if ((Action == Program.Action.Create || Action == Program.Action.Update))
            {
                if (string.IsNullOrWhiteSpace(ConfigFile))
                {
                    throw new ArgumentException("ConfigFile cannot be empty.");
                }
                if (!File.Exists(ConfigFile))
                {
                    throw new ArgumentException($"ConfigFile '{ConfigFile}' doesn't exist.");
                }
            }
        }
    }

    static void ShowUsageAndExit(int exitCode = 0)
    {
        var usage = @"
Usage:
{0} {{--create --config <cluster config file> | --update <cluster id> --config <cluster config file> | --use <clusetr id> | --delete <cluster id>}} [--debug] [--help | -h]
";
        Console.WriteLine(string.Format(usage, typeof(Program).Assembly.GetName().Name));
        Environment.Exit(exitCode);
    }

    static Options ParseCommandLine(string[] args)
    {
        var options = new Options();
        try
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--create":
                        options.Action = Action.Create;
                        break;
                    case "--update":
                        options.Action = Action.Update;
                        options.Id = args[++i];
                        break;
                    case "--use":
                        options.Action = Action.Use;
                        options.Id = args[++i];
                        break;
                    case "--delete":
                        options.Action = Action.Delete;
                        options.Id = args[++i];
                        break;
                    case "--config":
                        options.ConfigFile = args[++i];
                        break;
                    case "--debug":
                        options.Debug = true;
                        break;
                    case "-h":
                    case "--help":
                        ShowUsageAndExit(0);
                        break;
                    default:
                        throw new ArgumentException("Unkown argument!", args[i]);
                }
            }
            options.Validate();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ShowUsageAndExit(1);
        }
        return options;
    }

    static void CreateCluster(string configFile)
    {
        var config = GetClusterConfig(configFile);
        var logger = LoggerFactory.CreateLogger<Cluster>();
        var cluster = new Cluster(Credential, config, logger);
        cluster.CreateOrUpdateAsync().Wait();

        ShowClusterProperties(cluster);
    }

    static void UpdateCluster(string id, string configFile)
    {
        var config = GetClusterConfig(configFile);
        var logger = LoggerFactory.CreateLogger<Cluster>();
        var cluster = new Cluster(Credential, config, id, logger);
        cluster.CreateOrUpdateAsync().Wait();

        ShowClusterProperties(cluster);
    }

    static void UseCluster(string id)
    {
        var logger = LoggerFactory.CreateLogger<Cluster>();
        var cluster = new Cluster(Credential, id, logger);
        cluster.ValidateAsync().Wait();

        ShowClusterProperties(cluster);
    }

    static void DeleteCluster(string id)
    {
        var logger = LoggerFactory.CreateLogger<Cluster>();
        var cluster = new Cluster(Credential, id, logger);
        cluster.DestroyAsync().Wait();
    }

    static void ShowClusterProperties(Cluster cluster)
    {
        Console.WriteLine($"ClusterID={cluster.Id}");

        var properties = cluster.GetPropertiesAsync().Result;
        Console.WriteLine($"Service={properties?.ServiceProperties?.Service}");
        Console.WriteLine($"Queue:Type={properties?.QueueProperties?.QueueType}");
        Console.WriteLine($"Queue:RequestQueueName={properties?.QueueProperties?.RequestQueueName}");
        Console.WriteLine($"Queue:ResponseQueueName={properties?.QueueProperties?.ResponseQueueName}");
        Console.WriteLine($"Queue:ConnectionString={properties?.QueueProperties?.ConnectionString}");
    }

    static ClusterConfig GetClusterConfig(string configFile)
    {
        var content = File.ReadAllText(configFile);
        var config = JsonSerializer.Deserialize<ClusterConfig>(content);
        if (config == null)
        {
            throw new InvalidDataException("Invalid cluster configuration!");
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
        var options = ParseCommandLine(args);
        DebugOut = options.Debug;
        switch (options.Action)
        {
            case Action.Create:
                CreateCluster(options.ConfigFile!);
                break;
            case Action.Update:
                UpdateCluster(options.Id!, options.ConfigFile!);
                break;
            case Action.Use:
                UseCluster(options.Id!);
                break;
            case Action.Delete:
                DeleteCluster(options.Id!);
                break;
        }
    }
}
