using Cloud.Soa;
using CommandLine;
using System.Diagnostics;

namespace Receive;

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

        [Option('m', "max-messages", HelpText = "Max number of messages to receive before exit")]
        public int? MaxMessages { get; set; }

        [Option('i', "query-interval", Default = (int)500, HelpText = "For storage queue only")]
        public int QueryInterval { get; set; }

        [Option('v', "verbose")]
        public bool Verbose { get; set; }

        [Option('q', "quiet")]
        public bool Quiet { get; set; }

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

    static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<Options>(args)
            .MapResult(RunAsync, _ => Task.FromResult(1));
    }

    static async Task<int> RunAsync(Options options)
    {
        options.Validate();

        var queueOptions = new StorageQueueOptions()
        {
            QueueType = options.QueueType,
            ConnectionString = options.ConnectionString,
            QueueName = options.QueueName,
            MessageLease = 60,
            QueryInterval = options.QueryInterval,
        };
        var client = CreateQueueClient(queueOptions);

        Console.WriteLine($"Receive message from queue {options.QueueName}.");

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        Console.CancelKeyPress += (sender, args) => {
            args.Cancel = true;
            cts.Cancel();
        };

        int nReceived = 0;
        var stopWatch = new Stopwatch();

        Console.WriteLine($"Start receiving at {DateTimeOffset.Now}.");

        while (!token.IsCancellationRequested)
        {
            IMessage? message = null;
            try
            {
                message = await client.WaitAsync(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!options.Quiet)
            {
                Console.WriteLine($"Received a message of length {message.Content.Length} at {DateTimeOffset.Now}.");
                if (options.Verbose)
                {
                    Console.WriteLine(message.Content);
                }
                Console.WriteLine($"Delete message.");
            }
            await message.DeleteAsync();

            ++nReceived;
            if (nReceived == 1)
            {
                stopWatch.Start();
            }
            if (nReceived == options.MaxMessages)
            {
                break;
            }
        }
        stopWatch.Stop();

        Console.WriteLine($"End receiving at {DateTimeOffset.Now}.");
        Console.WriteLine($"Received {nReceived} messages.");
        if (nReceived > 1)
        {
            var throughput = (nReceived - 1) / stopWatch.Elapsed.TotalSeconds;
            Console.WriteLine($"Receive throughput: {throughput:f3} messages/second");
        }
        return 0;
    }
}
