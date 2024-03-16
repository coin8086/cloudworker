using Cloud.Soa.E2E;
using Cloud.Soa.MessageQueue;
using CommandLine;
using System.Diagnostics;

namespace Receive;

class Program
{
    class Options : Cloud.Soa.E2E.QueueOptions
    {
        [Option('n', "queue-name", Default = "responses")]
        public new string? QueueName { get; set; }

        [Option('m', "max-messages", HelpText = "Max number of messages to receive before exit")]
        public int? MaxMessages { get; set; }

        [Option('v', "verbose")]
        public bool Verbose { get; set; }
    }

    static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<Options>(args)
            .MapResult(RunAsync, _ => Task.FromResult(1));
    }

    static async Task<int> RunAsync(Options options)
    {
        options.Validate();
        Console.WriteLine($"Receive message from queue {options.QueueName}.");
        var client = QueueClient.Create(options);

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        Console.CancelKeyPress += (sender, args) => {
            args.Cancel = true;
            cts.Cancel();
        };

        int nReceived = 0;
        Console.WriteLine($"Start receiving at {DateTimeOffset.Now}.");

        while (!token.IsCancellationRequested)
        {
            IMessage? message = null;
            try
            {
                message = await client.WaitAsync(cancel: token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            Console.WriteLine($"Received a message of length {message.Content.Length} at {DateTimeOffset.Now}.");
            if (options.Verbose)
            {
                Console.WriteLine(message.Content);
            }

            Console.WriteLine($"Delete message.");
            await message.DeleteAsync();

            ++nReceived;
            if (nReceived == options.MaxMessages)
            {
                break;
            }
        }

        Console.WriteLine($"End receiving at {DateTimeOffset.Now}.");
        Console.WriteLine($"Received {nReceived} messages.");
        return 0;
    }
}
