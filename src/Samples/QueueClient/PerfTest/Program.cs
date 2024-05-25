using CloudWorker.MessageQueue;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PerfTest;

class Program
{
    class Options
    {
        public const string ENV_CONNECTION_STRING = "QUEUE_CONNECTION_STRING";

        public string? ConnectionString { get; set; }

        public string QueueType { get; set; } = ServiceBusQueue.QueueType;

        public string RequestQueueName { get; set; } = "requests";

        public string ResponseQueueName { get; set; } = "responses";

        //For storage queue only
        public int QueryInterval { get; set; } = 500;

        public int MessageLength { get; set; } = 4;

        public string? Message { get; set; }

        //Number of messages to send and/or receive
        public int Count { get; set; } = 2000;

        public int SenderCount { get; set; } = 10;

        public int ReceiverCount { get; set; } = 100;

        //Max number of messages to receive in one receive call
        public int BatchSize { get; set; } = 1;

        //From trace(0) to none(6)
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                var connectionString = Environment.GetEnvironmentVariable(ENV_CONNECTION_STRING);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new ArgumentException($"Connection string is missing! Set it in command line or in environment variable {ENV_CONNECTION_STRING}!");
                }
                ConnectionString = connectionString;
            }
            if (!StorageQueue.QueueType.Equals(QueueType, StringComparison.OrdinalIgnoreCase) &&
                !ServiceBusQueue.QueueType.Equals(QueueType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid queue type '{QueueType}'.", nameof(QueueType));
            }
            if (string.IsNullOrEmpty(Message) && MessageLength <= 0)
            {
                throw new ArgumentException("MessageLength must be greater than 0!");
            }
            if (Count <= 0)
            {
                throw new ArgumentException("Count must be greater than 0!");
            }
            if (SenderCount > 0 && Count < SenderCount)
            {
                throw new ArgumentException("Count cannot be less than SenderCount!");
            }
            if (SenderCount > 0 && string.IsNullOrWhiteSpace(RequestQueueName))
            {
                throw new ArgumentException("RequestQueueName cannot be empty!");
            }
            if (ReceiverCount > 0 && string.IsNullOrWhiteSpace(ResponseQueueName))
            {
                throw new ArgumentException("ResponseQueueName cannot be empty!");
            }
        }
    }

    static void ShowUsageAndExit(int exitCode = 0)
    {
        var usage = @"
Usage:
{0} --connect <queue connection string> [--queue-type <type>] [--request-queue <name>] [--response-queue <name>] [--query-interval <time in ms>] [--message-length <num in bytes>] [--message <content>] [--count <num of messages to send and/or receive>] [--senders <num>] [--receivers <num>] [--batch-size <size>] [--help | -h]

Note:
The connection string can also be set by environment variable {1}.
";
        Console.WriteLine(string.Format(usage, typeof(Program).Assembly.GetName().Name, Options.ENV_CONNECTION_STRING));
        Environment.Exit(exitCode);
    }

    static Options ParseCommandLine(string[] args)
    {
        var options = new Options();
        try
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--connect":
                        options.ConnectionString = args[++i];
                        break;
                    case "--queue-type":
                        options.QueueType = args[++i];
                        break;
                    case "--request-queue":
                        options.RequestQueueName = args[++i];
                        break;
                    case "--response-queue":
                        options.ResponseQueueName = args[++i];
                        break;
                    case "--query-interval":
                        options.QueryInterval = int.Parse(args[++i]);
                        break;
                    case "--message-length":
                        options.MessageLength = int.Parse(args[++i]);
                        break;
                    case "--message":
                        options.Message = args[++i];
                        break;
                    case "--count":
                        options.Count = int.Parse(args[++i]);
                        break;
                    case "--senders":
                        options.SenderCount = int.Parse(args[++i]);
                        break;
                    case "--receivers":
                        options.ReceiverCount = int.Parse(args[++i]);
                        break;
                    case "--batch-size":
                        options.BatchSize = int.Parse(args[++i]);
                        break;
                    case "-h":
                    case "--help":
                        ShowUsageAndExit(0);
                        break;
                    default:
                        throw new ArgumentException("Unkown argument!", args[i]);
                }
            }
            options.Validate();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ShowUsageAndExit(1);
        }
        return options;
    }

    //TODO: Refactor the following names
    static int MessagesToReceive = 0;
    static int MessagesReceived = 0;
    static int MessagesFailedSending = 0;
    static CancellationTokenSource Stop = new CancellationTokenSource();
    static ILoggerFactory? _LoggerFactory;
    static ILogger? _Logger;

    static async Task Main(string[] args)
    {
        var options = ParseCommandLine(args);
        await RunAsync(options);
    }

    static async Task<int> RunAsync(Options options)
    {
        options.Validate();

        _LoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddFilter("Default", options.LogLevel);
            builder.AddSimpleConsole(options =>
            {
                options.UseUtcTimestamp = true;
                options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
            });
        });
        _Logger = _LoggerFactory.CreateLogger<Program>();

        if (options.SenderCount > 0)
        {
            var messagesToSend = (options.Count / options.SenderCount) * options.SenderCount;
            options.Count = messagesToSend;
            MessagesToReceive = messagesToSend;
        }
        else
        {
            MessagesToReceive = options.Count;
        }

        Console.WriteLine($"Log level: {options.LogLevel}");
        Console.WriteLine($"Messages to send and/or receive: {options.Count}");
        Console.WriteLine($"Message length: {options.MessageLength}");
        Console.WriteLine($"Message queue type: {options.QueueType}");
        Console.WriteLine($"Sender count: {options.SenderCount}");
        Console.WriteLine($"Send to: {options.RequestQueueName}");
        Console.WriteLine($"Receiver count: {options.ReceiverCount}");
        Console.WriteLine($"Receive batch size: {options.BatchSize}");
        Console.WriteLine($"Receive from: {options.ResponseQueueName}");

        var tasks = new List<Task>(2);
        var sw = Stopwatch.StartNew();

        if (options.SenderCount > 0)
        {
            tasks.Add(StartSending(options));
        }
        if (options.ReceiverCount > 0)
        {
            tasks.Add(StartReceiving(options));
        }
        if (tasks.Count > 0)
        {
            Console.WriteLine($"Started at: {DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}");
            Console.WriteLine("Press any key to exit early...");
            _ = Task.Run(() =>
            {
                Console.ReadKey(true);
                Stop.Cancel();
            });
            await Task.WhenAll(tasks);
        }
        sw.Stop();

        if (tasks.Count > 0)
        {
            double throughput;
            if (options.ReceiverCount > 0)
            {
                throughput = MessagesReceived / sw.Elapsed.TotalSeconds;
            }
            else
            {
                throughput = (options.Count - MessagesFailedSending) / sw.Elapsed.TotalSeconds;
            }

            Console.WriteLine($"Stopped at: {DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}");
            Console.WriteLine($"Number of messages to send and/or receive: {options.Count}");
            Console.WriteLine($"Number of messages failed being sent: {MessagesFailedSending}");
            Console.WriteLine($"Adjusted number of messages to receive: {MessagesToReceive}");
            Console.WriteLine($"Actual number of messages received: {MessagesReceived}");
            Console.WriteLine($"Time elapsed: {sw.Elapsed}");
            Console.WriteLine($"End-to-end effective throughput: {throughput:f3} messages/second");
        }
        return 0;
    }

    static Task StartSending(Options options)
    {
        var message = string.IsNullOrEmpty(options.Message) ?  new String('a', options.MessageLength) : options.Message;
        var batch = options.Count / options.SenderCount;
        var logger = _LoggerFactory!.CreateLogger("Sender");
        var tasks = new Task[options.SenderCount];
        for (var i = 0; i < options.SenderCount; i++)
        {
            var sender = CreateSender(options, logger);
            tasks[i] = StartSender(sender, message, batch);
        }
        return Task.WhenAll(tasks);
    }

    static Task StartSender(IMessageQueue sender, string message, int count)
    {
        var tasks = new Task[count];
        for (var i = 0; i < count; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    await sender.SendAsync(message);
                }
                catch (Exception ex)
                {
                    _Logger!.LogWarning(ex, "Error in sending a message");
                    Interlocked.Increment(ref MessagesFailedSending);
                    Interlocked.Decrement(ref MessagesToReceive);
                    if (MessagesReceived >= MessagesToReceive)
                    {
                        Stop.Cancel();
                    }
                }
            });
        }
        return Task.WhenAll(tasks);
    }

    static Task StartReceiving(Options options)
    {
        var logger = _LoggerFactory!.CreateLogger("Receiver");
        var tasks = new Task[options.ReceiverCount];
        for (var i = 0; i < options.ReceiverCount; i++)
        {
            var receiver = CreateReceiver(options, logger);
            tasks[i] = StartReceiver(receiver, options.BatchSize);
        }
        return Task.WhenAll(tasks);
    }

    static async Task StartReceiver(IMessageQueue receiver, int batchSize)
    {
        while (!Stop.IsCancellationRequested)
        {
            IReadOnlyList<IQueueMessage>? messages = null;
            try
            {
                messages = await receiver.WaitBatchAsync(batchSize, Stop.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            var tasks = new Task[messages.Count];
            for (var i = 0; i <  messages.Count; i++)
            {
                var message = messages[i];
                tasks[i] = Task.Run(async() =>
                {
                    try
                    {
                        await message.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        _Logger!.LogWarning(ex, "Error in deleting a message");
                    }
                    Interlocked.Increment(ref MessagesReceived);
                    //NOTE: When the initial request and/or response queues are not empty and batchSize is greater than one,
                    //then more messages than MessagesToReceive may be received.
                    if (MessagesReceived >= MessagesToReceive)
                    {
                        Stop.Cancel();
                    }
                });
            }
            //TODO: No wait before receiving a new message for higher performance
            await Task.WhenAll(tasks);
        }
    }

    static IMessageQueue CreateQueueClient(string queueType, string connectionString, string queueName, ILogger? logger = null)
    {
        if (ServiceBusQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            var queueOptions = new ServiceBusQueueOptions()
            {
                QueueType = queueType,
                ConnectionString = connectionString,
                QueueName = queueName,
                MessageLease = 60,
            };
            return new ServiceBusQueue(queueOptions, logger);
        }
        else if (StorageQueue.QueueType.Equals(queueType, StringComparison.OrdinalIgnoreCase))
        {
            var queueOptions = new StorageQueueOptions()
            {
                QueueType = queueType,
                ConnectionString = connectionString,
                QueueName = queueName,
                MessageLease = 60,
            };
            return new StorageQueue(queueOptions);
        }
        else
        {
            throw new ArgumentException($"Invalid queue type {queueType}");
        }
    }

    static IMessageQueue CreateSender(Options options, ILogger? logger = null)
    {
        return CreateQueueClient(options.QueueType, options.ConnectionString!, options.RequestQueueName, logger);
    }

    static IMessageQueue CreateReceiver(Options options, ILogger? logger = null)
    {
        return CreateQueueClient(options.QueueType, options.ConnectionString!, options.ResponseQueueName, logger);
    }
}
