using Azure.Storage.Queues;

namespace SendRequest;

class Program
{
    const string ENV_CONNECTION_STRING = "STORAGE_CONNECTION_STRING";

    static void ShowUsage()
    {
        var usage = @"
SendRequest -c {count} -m {message} -q {queue}

or

SendRequest -c {count} -q {queue} -

A single ""-"" means the message is read from stdin.
";
        Console.WriteLine(usage);
    }

    static (int count, string message, string queue) ParseCommandLine()
    {
        var args = Environment.GetCommandLineArgs();
        int count = 0;
        string? message = null;
        string? queueName = null;
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
                else if ("-q".Equals(args[i], StringComparison.Ordinal))
                {
                    queueName = args[++i];
                    if (string.IsNullOrEmpty(queueName))
                    {
                        throw new ArgumentException($"Parameter '-q <name>' must not be empty!");
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
        return (count, message, queueName);
    }

    static int Main(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ENV_CONNECTION_STRING);
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.Error.WriteLine($"Environment variable {ENV_CONNECTION_STRING} is required but missing!");
            return 1;
        }

        var (count, message, queueName) = ParseCommandLine();

        Console.WriteLine($"Create queue {queueName} if it doesn't exists.");
        var client = new QueueClient(connectionString, queueName);
        client.CreateIfNotExists();

        Console.WriteLine($"Send {count} messages, each of length {message.Length}, to the queue.");
        var tasks = new Task[count];
        Console.WriteLine($"Start sending at {DateTimeOffset.Now}");
        for (int i = 0; i < count; i++)
        {
            tasks[i] = client.SendMessageAsync(message);
        }
        Task.WaitAll(tasks);
        Console.WriteLine($"End sending at {DateTimeOffset.Now}");
        return 0;
    }
}
