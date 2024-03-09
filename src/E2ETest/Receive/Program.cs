using Cloud.Soa;
using Cloud.Soa.Client;
using CommandLine;
using System.Diagnostics;

namespace Receive;

class Program
{
    class Options : Cloud.Soa.Client.StorageQueueOptions
    {
        [Option('m', "max-messages", HelpText = "Max number of messages to receive before exit")]
        public int? MaxMessages { get; set; }

        [Option('v', "verbose")]
        public bool Verbose { get; set; }

        [Option('q', "quiet")]
        public bool Quiet { get; set; }
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
