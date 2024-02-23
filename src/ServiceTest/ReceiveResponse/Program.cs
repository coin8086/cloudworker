using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace ReceiveResponse;

class Program
{
    const string ENV_CONNECTION_STRING = "STORAGE_CONNECTION_STRING";

    static void ShowUsage()
    {
        var usage = @"
ReceiveResponse -q {queue} [-v]
";
        Console.WriteLine(usage);
    }

    static (string queue, bool verbose) ParseCommandLine()
    {
        var args = Environment.GetCommandLineArgs();
        string? queueName = null;
        bool verbose = false;
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                if ("-q".Equals(args[i], StringComparison.Ordinal))
                {
                    queueName = args[++i];
                    if (string.IsNullOrEmpty(queueName))
                    {
                        throw new ArgumentException($"Parameter '-q <name>' must not be empty!");
                    }
                }
                if ("-v".Equals(args[i], StringComparison.Ordinal))
                {
                    verbose = true;
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
        return (queueName, verbose);
    }

    static int Main(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ENV_CONNECTION_STRING);
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.Error.WriteLine($"Environment variable {ENV_CONNECTION_STRING} is required but missing!");
            return 1;
        }

        var (queueName, verbose) = ParseCommandLine();

        Console.WriteLine($"Create queue {queueName} if it doesn't exists.");
        var client = new QueueClient(connectionString, queueName);
        client.CreateIfNotExists();

        Console.WriteLine($"Receive message from the queue.");

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        Console.CancelKeyPress += (sender, args) => {
            args.Cancel = true;
            cts.Cancel();
        };

        while (!token.IsCancellationRequested)
        {
            QueueMessage message = client.ReceiveMessage(cancellationToken: token);
            Console.WriteLine($"Received a message of length {message.MessageText} at {DateTimeOffset.Now}.");
            if (verbose)
            {
                Console.WriteLine(message.MessageText);
            }
        }
        return 0;
    }
}
