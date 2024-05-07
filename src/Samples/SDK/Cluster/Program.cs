using Azure.Identity;
using CloudWorker.Client.SDK;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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

    static void ShowUsageAndExit(int exitCode = 0)
    {
        var usage = @"
Usage: 
{0} {--create --config <cluster config file> | --update <cluster id> --config <cluster config file> | --use <clusetr id> | --delete <cluster id>} [--help | -h]
";
        Console.WriteLine(string.Format(usage, typeof(Program).Assembly.FullName));
        Environment.Exit(exitCode);
    }

    static (Action action, string? clusterId, string? clusterConfigFile) ParseCommandLine(string[] args)
    {
        Action? action = null;
        string? clusterId = null;
        string? clusterConfigFile = null;

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
                        clusterId = args[++i];
                        break;
                    case "--use":
                        action = Action.Use;
                        clusterId = args[++i];
                        break;
                    case "--delete":
                        action = Action.Delete;
                        clusterId = args[++i];
                        break;
                    case "--config":
                        clusterConfigFile = args[++i];
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
            if (action != Action.Create && clusterId == null)
            {
                throw new ArgumentException($"Cluster ID is requried for action {action}.");
            }
            if ((action == Action.Create || action == Action.Update))
            {
                if (string.IsNullOrWhiteSpace(clusterConfigFile))
                {
                    throw new ArgumentException("Cluster configuration file must be specified.", "--config");
                }
                if (!File.Exists(clusterConfigFile))
                {
                    throw new ArgumentException("Cluster configuration file doesn't exist.", "--config");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ShowUsageAndExit(1);
        }
        Debug.Assert(action != null);
        return (action.Value, clusterId, clusterConfigFile);
    }

    static void CreateCluster(string configFile)
    {
        var config = GetClusterConfig(configFile);
        var logger = LoggerFactory.CreateLogger<Cluster>();
        var cluster = new Cluster(new DefaultAzureCredential(), config, logger);
        cluster.CreateOrUpdateAsync().Wait();

        ShowClusterProperties(cluster);
    }

    static void UpdateCluster(string id, string configFile)
    {
        var config = GetClusterConfig(configFile);
        var logger = LoggerFactory.CreateLogger<Cluster>();
        var cluster = new Cluster(new DefaultAzureCredential(), config, id, logger);
        cluster.CreateOrUpdateAsync().Wait();

        ShowClusterProperties(cluster);
    }

    static void UseCluster(string id)
    {
        var logger = LoggerFactory.CreateLogger<Cluster>();
        var cluster = new Cluster(new DefaultAzureCredential(), id, logger);
        cluster.ValidateAsync().Wait();

        ShowClusterProperties(cluster);
    }

    static void DeleteCluster(string id)
    {
        var logger = LoggerFactory.CreateLogger<Cluster>();
        var cluster = new Cluster(new DefaultAzureCredential(), id, logger);
        cluster.DestroyAsync().Wait();
    }

    static void ShowClusterProperties(Cluster cluster)
    {
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

    static Lazy<ILoggerFactory> _lazyLoggerFactory = new Lazy<ILoggerFactory>(() =>
    {
        return Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
            });
        });
    });

    static ILoggerFactory LoggerFactory => _lazyLoggerFactory.Value;

    static void Main(string[] args)
    {
        var (action, clusterId, configFile) = ParseCommandLine(args);
        switch (action)
        {
            case Action.Create:
                CreateCluster(configFile!);
                break;
            case Action.Update:
                UpdateCluster(clusterId!, configFile!);
                break;
            case Action.Delete:
                DeleteCluster(clusterId!);
                break;
            case Action.Use:
                UseCluster(clusterId!);
                break;
        }
    }
}
