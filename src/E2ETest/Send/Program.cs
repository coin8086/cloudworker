using CloudWork.E2E;
using CommandLine;

namespace Send;

class Program
{
    class Options : CloudWork.E2E.QueueOptions
    {
        [Option('n', "queue-name", Default = "requests")]
        public override string? QueueName { get; set; }

        [Option('c', "count", Default = (int)1, HelpText = "Number of repeat sendings (for the same message)")]
        public int Count { get; set; }

        [Option('m', "message", HelpText = "Message to send. Stdin is read as message content if this option is not specified.")]
        public string? Message { get; set; }

        public override void Validate()
        {
            base.Validate();
            if (Message == null)
            {
                using var stdin = Console.OpenStandardInput();
                using var reader = new StreamReader(stdin);
                Message = reader.ReadToEnd();
            }
        }
    }

    static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<Options>(args).MapResult(Run, _ => 1);
    }

    static int Run(Options options)
    {
        options.Validate();
        var client = QueueClient.Create(options);

        var tasks = new Task[options.Count];

        Console.WriteLine($"Send {options.Count} messages, each of length {options.Message!.Length}, to queue {options.QueueName}.");
        Console.WriteLine($"Started at {DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}");

        for (int i = 0; i < options.Count; i++)
        {
            tasks[i] = client.SendAsync(options.Message!);
        }
        Task.WaitAll(tasks);

        Console.WriteLine($"Stopped at {DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}");
        return 0;
    }
}
