using Cloud.Soa;
using System.Diagnostics;

namespace Receive;

class Program
{
    const string ENV_CONNECTION_STRING = "QUEUE_CONNECTION_STRING";

    static void ShowUsage()
    {
        var usage = @"
Receive [-t {storage|servicebus}] -n {queue name} [-m {max number of messages to receive}] [-i {query interval}] [-v] [-q]
";
        Console.WriteLine(usage);
    }

    static (string? queueType, string queue, int? maxMessages, int queryInterval, bool verbose, bool quiet)
        ParseCommandLine(string[] args)
    {
        string? queueType = null;
        string? queueName = null;
        int? maxMessages = null;
        int queryInterval = 200;
        bool verbose = false;
        bool quiet = false;
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                if ("-t".Equals(args[i], StringComparison.Ordinal))
                {
                    queueType = args[++i];
                    if (!"storage".Equals(queueType, StringComparison.OrdinalIgnoreCase) &&
                        !"servicebus".Equals(queueType, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("Unrecognized queue type!");
                    }
                }
                else if ("-n".Equals(args[i], StringComparison.Ordinal))
                {
                    queueName = args[++i];
                    if (string.IsNullOrEmpty(queueName))
                    {
                        throw new ArgumentException("Queue name must be specified!");
                    }
                }
                else if ("-m".Equals(args[i], StringComparison.Ordinal))
                {
                    maxMessages = int.Parse(args[++i]);
                    if (maxMessages < 0)
                    {
                        throw new ArgumentException("-m {number} must be greater than 0!");
                    }
                }
                else if ("-i".Equals(args[i], StringComparison.Ordinal))
                {
                    queryInterval = int.Parse(args[++i]);
                    if (queryInterval < 0)
                    {
                        throw new ArgumentException("-i {number} must be greater than 0!");
                    }
                }
                else if ("-v".Equals(args[i], StringComparison.Ordinal))
                {
                    verbose = true;
                }
                else if ("-q".Equals(args[i], StringComparison.Ordinal))
                {
                    quiet = true;
                }
                else if ("-h".Equals(args[i], StringComparison.Ordinal))
                {
                    ShowUsage();
                    Environment.Exit(0);
                }
                else
                {
                    throw new ArgumentException($"Unrecognized parameter '{args[i]}'!");
                }
            }
            if (queueName == null)
            {
                throw new ArgumentException($"At least a required parameter is missing!");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error when parsing arguments: {ex}");
            ShowUsage();
            Environment.Exit(1);
        }
        return (queueType, queueName, maxMessages, queryInterval, verbose, quiet);
    }

    static IMessageQueue CreateQueueClient(StorageQueueOptions options)
    {
        if (string.IsNullOrEmpty(options.QueueType) ||
            string.Equals(options.QueueType, "servicebus", StringComparison.OrdinalIgnoreCase))
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
        var connectionString = Environment.GetEnvironmentVariable(ENV_CONNECTION_STRING);
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.Error.WriteLine($"Environment variable {ENV_CONNECTION_STRING} is required but missing!");
            return 1;
        }

        var (queueType, queueName, maxMessages, queryInterval, verbose, quiet) = ParseCommandLine(args);

        var queueOptions = new StorageQueueOptions()
        {
            QueueType = queueType,
            ConnectionString = connectionString,
            QueueName = queueName,
            MessageLease = 60,
            QueryInterval = queryInterval,
        };
        var client = CreateQueueClient(queueOptions);

        Console.WriteLine($"Receive message from queue {queueName}.");

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

            if (!quiet)
            {
                Console.WriteLine($"Received a message of length {message.Content.Length} at {DateTimeOffset.Now}.");
                if (verbose)
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
            if (nReceived == maxMessages)
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
