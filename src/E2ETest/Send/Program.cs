using Cloud.Soa.E2E;
using CommandLine;
using System.Diagnostics;

namespace Send;

class Program
{
    class Options : Cloud.Soa.E2E.QueueOptions
    {
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
