using CloudWorker.MessageQueue;

namespace SendAndReceive;

class Program
{
    class Options
    {
        public const string ENV_CONNECTION_STRING = "QUEUE_CONNECTION_STRING";

        public string? ConnectionString { get; set; }

        public string QueueType { get; set; } = ServiceBusQueue.QueueType;

        public string RequestQueueName { get; set; } = "requests";

        public string ResponseQueueName { get; set; } = "responses";

        public string? Message { get; set; }

        public int Count { get; set; } = 1;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                var connectionString = Environment.GetEnvironmentVariable(ENV_CONNECTION_STRING);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new ArgumentException($"ConnectionString cannot be empty. Set it in command line or in environment variable {ENV_CONNECTION_STRING}.",
                        nameof(ConnectionString));
                }
                ConnectionString = connectionString;
            }
            if (!StorageQueue.QueueType.Equals(QueueType, StringComparison.OrdinalIgnoreCase) &&
                !ServiceBusQueue.QueueType.Equals(QueueType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid queue type '{QueueType}'.", nameof(QueueType));
            }
            if (string.IsNullOrEmpty(Message))
            {
                throw new ArgumentException("Cannot be empty.", nameof(Message));
            }
            if (Count < 1)
            {
                throw new ArgumentException($"Count is {Count}, less than 1.", nameof(Count));
            }
        }
    }

    static void ShowUsageAndExit(int exitCode = 0)
    {
        var usage = @"
Usage:
{0} --connect <queue connection string> [--queue-type <type>] [--request-queue <name>] [--response-queue <name>] --message <content> [--count <num>] [--help | -h]

Note:
The connection string can also be set by environment variable {1}.
";
        Console.WriteLine(string.Format(usage, typeof(Program).Assembly.GetName().Name, Options.ENV_CONNECTION_STRING));
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
                    case "--connect":
                        options.ConnectionString = args[++i];
                        break;
                    case "--queue-type":
                        options.QueueType = args[++i];
                        break;
                    case "--request-queue":
                        options.RequestQueueName = args[++i];
                        break;
                    case "--response-queue":
                        options.ResponseQueueName = args[++i];
                        break;
                    case "--message":
                        options.Message = args[++i];
                        break;
                    case "--count":
                        options.Count = int.Parse(args[++i]);
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

    static IMessageQueue CreateQueueClient(string queueType, string connectionString, string queueName)
    {
        if (ServiceBusQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            var queueOptions = new ServiceBusQueueOptions()
            {
                QueueType = queueType,
                ConnectionString = connectionString,
                QueueName = queueName,
                MessageLease = 60,
            };
            return new ServiceBusQueue(queueOptions);
        }
        else if (StorageQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            var queueOptions = new StorageQueueOptions()
            {
                QueueType = queueType,
                ConnectionString = connectionString,
                QueueName = queueName,
                MessageLease = 60,
            };
            return new StorageQueue(queueOptions);
        }
        else
        {
            throw new ArgumentException($"Invalid queue type {queueType}");
        }
    }

    static IMessageQueue CreateSender(Options options)
    {
        return CreateQueueClient(options.QueueType, options.ConnectionString!, options.RequestQueueName);
    }

    static IMessageQueue CreateReceiver(Options options)
    {
        return CreateQueueClient(options.QueueType, options.ConnectionString!, options.ResponseQueueName);
    }

    static void Main(string[] args)
    {
        var options = ParseCommandLine(args);

        Console.WriteLine("Sending messages");
        var sender = CreateSender(options);
        var sendingTasks = new Task[options.Count];
        for (var i = 0; i < options.Count; i++)
        {
            sendingTasks[i] = sender.SendAsync(options.Message!);
        }

        //Optionally wait before receiving
        //Task.WaitAll(sendingTasks);

        Console.WriteLine("Receiving messages");
        var receiver = CreateReceiver(options);
        var receivingTasks = new Task[options.Count];
        for (var i = 0; i < options.Count; i++)
        {
            receivingTasks[i] = receiver.WaitAsync().ContinueWith(task =>
            {
                var reply = task.Result;
                Console.WriteLine(reply.Content);
                reply.DeleteAsync().Wait();
            });
        }

        var tasks = sendingTasks.Concat(receivingTasks).ToArray();
        Task.WaitAll(tasks);
    }
}
