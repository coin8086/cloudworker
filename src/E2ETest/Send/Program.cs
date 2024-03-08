using Cloud.Soa;
using CommandLine;
using System.Diagnostics;

namespace Send;

class Program
{
    class Options
    {
        const string ENV_CONNECTION_STRING = "QUEUE_CONNECTION_STRING";

        [Option('C', "connection-string", HelpText = $"Can also be set by env var '{ENV_CONNECTION_STRING}'")]
        public string? ConnectionString { get; set; }

        [Option('t', "queue-type", HelpText = $"Can be '{ServiceBusQueue.QueueType}' or '{StorageQueue.QueueType}'.", Default = (string)ServiceBusQueue.QueueType)]
        public string? QueueType { get; set; }

        [Option('n', "queue-name", Required = true)]
        public string? QueueName { get; set; }

        [Option('c', "count", Default = (int)1, HelpText = "Number of repeat sendings (for the same message)")]
        public int Count { get; set; }

        [Option('m', "message", HelpText = "Message to send. Stdin is read as message content if this option is not specified.")]
        public string? Message { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                var connectionString = Environment.GetEnvironmentVariable(ENV_CONNECTION_STRING);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new ArgumentException($"Connection string is missing! Set it either in command line or in environment variable {ENV_CONNECTION_STRING}!");
                }
                ConnectionString = connectionString;
            }
            if (!string.IsNullOrEmpty(QueueType))
            {
                if (!QueueType.Equals(StorageQueue.QueueType, StringComparison.OrdinalIgnoreCase) &&
                    !QueueType.Equals(ServiceBusQueue.QueueType, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Invalid queue type '{QueueType}'!");
                }
            }
            if (Message == null)
            {
                using var stdin = Console.OpenStandardInput();
                using var reader = new StreamReader(stdin);
                Message = reader.ReadToEnd();
            }
        }
    }

    static IMessageQueue CreateQueueClient(StorageQueueOptions options)
    {
        if (string.IsNullOrEmpty(options.QueueType) ||
            string.Equals(options.QueueType, ServiceBusQueue.QueueType, StringComparison.OrdinalIgnoreCase))
        {
            return new ServiceBusQueue(options);
        }
        else
        {
            return new StorageQueue(options);
        }
    }

    static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<Options>(args).MapResult(Run, _ => 1);
    }

    static int Run(Options options)
    {
        options.Validate();

        var queueOptions = new StorageQueueOptions()
        {
            QueueType = options.QueueType,
            ConnectionString = options.ConnectionString,
            QueueName = options.QueueName,
            MessageLease = 60
        };
        var client = CreateQueueClient(queueOptions);

        var tasks = new Task[options.Count];
        var stopWatch = new Stopwatch();

        Console.WriteLine($"Send {options.Count} messages, each of length {options.Message!.Length}, to queue {options.QueueName}.");
        Console.WriteLine($"Start sending at {DateTimeOffset.Now}");

        stopWatch.Start();
        for (int i = 0; i < options.Count; i++)
        {
            tasks[i] = client.SendAsync(options.Message!);
        }
        Task.WaitAll(tasks);
        stopWatch.Stop();

        Console.WriteLine($"End sending at {DateTimeOffset.Now}");

        var throughput = options.Count / stopWatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Send throughput: {throughput:f3} messages/second");
        return 0;
    }
}
