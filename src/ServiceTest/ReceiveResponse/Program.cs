using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace ReceiveResponse;

class Program
{
    const string ENV_CONNECTION_STRING = "STORAGE_CONNECTION_STRING";

    static void ShowUsage()
    {
        var usage = @"
ReceiveResponse -n {queue name} [-v] [-q]
";
        Console.WriteLine(usage);
    }

    static (string queue, bool verbose, bool quiet) ParseCommandLine(string[] args)
    {
        string? queueName = null;
        bool verbose = false;
        bool quiet = false;
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                if ("-n".Equals(args[i], StringComparison.Ordinal))
                {
                    queueName = args[++i];
                    if (string.IsNullOrEmpty(queueName))
                    {
                        throw new ArgumentException("Queue name must be specified!");
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
        return (queueName, verbose, quiet);
    }

    static int Main(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ENV_CONNECTION_STRING);
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.Error.WriteLine($"Environment variable {ENV_CONNECTION_STRING} is required but missing!");
            return 1;
        }

        var (queueName, verbose, quiet) = ParseCommandLine(args);

        if (!quiet)
        {
            Console.WriteLine($"Create queue {queueName} if it doesn't exists.");
        }
        var client = new QueueClient(connectionString, queueName);
        client.CreateIfNotExists();

        Console.WriteLine($"Receive message from queue {queueName}.");

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        Console.CancelKeyPress += (sender, args) => {
            args.Cancel = true;
            cts.Cancel();
        };

        int count = 0;
        while (!token.IsCancellationRequested)
        {
            QueueMessage message = client.ReceiveMessage(cancellationToken: token);
            if (message == null)
            {
                if (!quiet && verbose)
                {
                    Console.WriteLine("No message. Wait.");
                }
                try
                {
                    Task.Delay(1000).Wait(token);
                }
                catch (OperationCanceledException) {}
                continue;
            }

            ++count;

            if (!quiet)
            {
                Console.WriteLine($"Received a message of length {message.MessageText.Length} at {DateTimeOffset.Now}.");
                if (verbose)
                {
                    Console.WriteLine(message.MessageText);
                }
                Console.WriteLine($"Delete message.");
            }
            client.DeleteMessage(message.MessageId, message.PopReceipt);
        }
        Console.WriteLine($"Received {count} messages totally.");
        return 0;
    }
}
