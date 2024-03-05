using Cloud.Soa;
using System.Diagnostics;

namespace Send;

class Program
{
    const string ENV_CONNECTION_STRING = "QUEUE_CONNECTION_STRING";

    static void ShowUsage()
    {
        var usage = @"
Send [-t {storage|servicebus}] -n {queue name} -c {count} -m {message}

or

Send [-t {storage|servicebus}] -n {queue name} -c {count} -

A single ""-"" means the message is read from stdin.
";
        Console.WriteLine(usage);
    }

    static (string? queueType, string queue, int count, string message) ParseCommandLine(string[] args)
    {
        int count = 0;
        string? message = null;
        string? queueName = null;
        string? queueType = null;
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                if ("-c".Equals(args[i], StringComparison.Ordinal))
                {
                    count = int.Parse(args[++i]);
                    if (count <= 0)
                    {
                        throw new ArgumentException($"Parameter '-c <count>' must be greater than 0!");
                    }
                }
                else if ("-m".Equals(args[i], StringComparison.Ordinal))
                {
                    message = args[++i];
                }
                else if ("-".Equals(args[i], StringComparison.Ordinal))
                {
                    using var stdin = Console.OpenStandardInput();
                    using var reader = new StreamReader(stdin);
                    message = reader.ReadToEnd();
                }
                else if ("-n".Equals(args[i], StringComparison.Ordinal))
                {
                    queueName = args[++i];
                    if (string.IsNullOrEmpty(queueName))
                    {
                        throw new ArgumentException("Queue name must be specified!");
                    }
                }
                else if ("-t".Equals(args[i], StringComparison.Ordinal))
                {
                    queueType = args[++i];
                    if (!"storage".Equals(queueType, StringComparison.OrdinalIgnoreCase) && 
                        !"servicebus".Equals(queueType, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("Unrecognized queue type!");
                    }
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
            if (count == 0 || message == null || queueName == null)
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
        return (queueType, queueName, count, message);
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

    static int Main(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ENV_CONNECTION_STRING);
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.Error.WriteLine($"Environment variable {ENV_CONNECTION_STRING} is required but missing!");
            return 1;
        }

        var (queueType, queueName, count, message) = ParseCommandLine(args);

        var queueOptions = new StorageQueueOptions()
        {
            QueueType = queueType,
            ConnectionString = connectionString,
            QueueName = queueName,
            MessageLease = 60
        };
        var client = CreateQueueClient(queueOptions);

        var tasks = new Task[count];
        var stopWatch = new Stopwatch();

        Console.WriteLine($"Send {count} messages, each of length {message.Length}, to queue {queueName}.");
        Console.WriteLine($"Start sending at {DateTimeOffset.Now}");

        stopWatch.Start();
        for (int i = 0; i < count; i++)
        {
            tasks[i] = client.SendAsync(message);
        }
        Task.WaitAll(tasks);
        stopWatch.Stop();

        Console.WriteLine($"End sending at {DateTimeOffset.Now}");

        var throughput = count / stopWatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Send throughput: {throughput:f3} messages/second");
        return 0;
    }
}
