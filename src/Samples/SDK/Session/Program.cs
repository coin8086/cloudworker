using Azure.Core;
using Azure.Identity;
using CloudWorker.Client.SDK;
using CloudWorker.Services.GRpc.Client;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

using GRpcRequest = CloudWorker.Services.GRpc.Client.Request;

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

    class Options
    {
        public Action? Action { get; set; }

        public string? Id { get; set; }

        public string? ConfigFile { get; set; }

        public string? Message {  get; set; }

        public int Count { get; set; } = 1;

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
            if (Action == Program.Action.Delete && Message != null)
            {
                throw new ArgumentException("Cannot send message in delete action.");
            }
            if (Count < 1)
            {
                throw new ArgumentException("Count cannot be less than 1.");
            }
        }
    }

    static void ShowUsageAndExit(int exitCode = 0)
    {
        var usage = @"
Usage:
{0} {{--create --config <session config file> | --update <session id> --config <session config file> | --use <clusetr id> | --delete <session id>}} [--send <msg>] [--count <count>] [--debug] [--help | -h]
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
                    case "--send":
                        options.Message = args[++i];
                        break;
                    case "--count":
                        options.Count = int.Parse(args[++i]);
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

    static Session CreateSession(string configFile)
    {
        Logger.LogInformation("Create session");
        var config = GetConfig(configFile);
        var session = Session.CreateOrUpdateAsync(Credential, config, null, LoggerFactory).Result;
        Logger.LogInformation("Created session {id}", session.Id);
        return session;
    }

    static Session UpdateSession(string id, string configFile)
    {
        Logger.LogInformation("Update session {id}", id);
        var config = GetConfig(configFile);
        return Session.CreateOrUpdateAsync(Credential, config, id, LoggerFactory).Result;
    }

    static Session UseSession(string id)
    {
        Logger.LogInformation("Use session {id}", id);
        return Session.GetAsync(Credential, id, LoggerFactory).Result;
    }

    static void DeleteSession(string id)
    {
        Logger.LogInformation("Delete session {id}", id);
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

    static void SendAndReceiveMessage(Session session, string msg, int count)
    {
        Logger.LogInformation("Send message \"{msg}\" {count} time(s).", msg, count);
        var sender = session.CreateSender();
        var sendingTasks = new Task[count];
        for (var i = 0; i < count; i++)
        {
            sendingTasks[i] = sender.SendAsync(msg);
        }

        Logger.LogInformation("Receive messages");
        var receiver = session.CreateReceiver();
        var receivingTasks = new Task[count];
        for (var i = 0; i < count; i++)
        {
            receivingTasks[i] = Task.Run(async () =>
            {
                var reply = await receiver.WaitAsync();
                Logger.LogDebug(reply.Content);
                await reply.DeleteAsync();
            });
        }
        var tasks = sendingTasks.Concat(receivingTasks).ToArray();
        Task.WaitAll(tasks);
    }

    static void SendAndReceiveGRpcMessage(Session session, string msg, int count)
    {
        var service = session.ClusterProperties?.ServiceProperties?.Service;
        if (!"grpc".Equals(service))
        {
            Logger.LogError("grpc service is expected but '{service}' is.", service);
            return;
        }

        Logger.LogInformation("Send message \"{msg}\" {count} time(s).", msg, count);

        var sender = session.CreateSender();
        var sendingTasks = new Task[count];
        var gMethod = GRpcHello.Greeter.Descriptor.FindMethodByName("SayHello");
        var gMsg = new GRpcHello.HelloRequest() { Name = msg };
        var request = new GRpcRequest(gMethod, gMsg);

        Logger.LogDebug("Real message to send:\n{msg}", request.ToJson());

        for (var i = 0; i < count; i++)
        {
            sendingTasks[i] = sender.SendGRpcMessageAsync(request);
        }

        Logger.LogInformation("Receive messages");

        var receiver = session.CreateReceiver();
        var receivingTasks = new Task[count];

        for (var i = 0; i < count; i++)
        {
            receivingTasks[i] = Task.Run(async () =>
            {
                var reply = await receiver.WaitGRpcMessageAsync<GRpcHello.HelloReply>();
                if (reply.Error != null)
                {
                    Logger.LogError("Error: {error}", reply.Error);
                }
                else
                {
                    Logger.LogDebug("{msg}", reply.GRpcMessage);
                }
                await reply.DeleteAsync();
            });
        }
        var tasks = sendingTasks.Concat(receivingTasks).ToArray();
        Task.WaitAll(tasks);
    }

    static bool DebugOut { get; set; } = false;

    static Lazy<ILoggerFactory> _lazyLoggerFactory = new Lazy<ILoggerFactory>(() =>
    {
        return Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(options =>
            {
                options.UseUtcTimestamp = true;
                options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
            });
            if (DebugOut)
            {
                builder.SetMinimumLevel(LogLevel.Trace);
            }
        });
    });

    static ILoggerFactory LoggerFactory => _lazyLoggerFactory.Value;

    static ILogger Logger => LoggerFactory.CreateLogger<Program>();

    static TokenCredential Credential => new DefaultAzureCredential();

    static void Main(string[] args)
    {
        var options = ParseCommandLine(args);
        DebugOut = options.Debug;
        Session? session = null;
        switch (options.Action)
        {
            case Action.Create:
                session = CreateSession(options.ConfigFile!);
                break;
            case Action.Update:
                session = UpdateSession(options.Id!, options.ConfigFile!);
                break;
            case Action.Use:
                session = UseSession(options.Id!);
                break;
            case Action.Delete:
                DeleteSession(options.Id!);
                break;
        }
        if (session != null && options.Message != null)
        {
            var service = session.ClusterProperties?.ServiceProperties?.Service;
            Debug.Assert(service != null);
            if ("grpc".Equals(service, StringComparison.OrdinalIgnoreCase))
            {
                SendAndReceiveGRpcMessage(session, options.Message, options.Count);
            }
            else
            {
                SendAndReceiveMessage(session, options.Message, options.Count);
            }
        }
    }
}
