using CloudWorker.E2E;
using CloudWorker.MessageQueue;
using CloudWorker.Services.GRpc.Client;
using CommandLine;
using GRpcHello;

namespace GRpcHelloClient;

class Program
{
    class Options : CloudWorker.E2E.QueueOptions
    {
        [Option(Hidden = true, Required = false)]
        public override string? QueueName { get; set; }

        [Option('S', "request-queue", Default = (string)"requests")]
        public string? RequestQueueName { get; set; }

        [Option('R', "response-queue", Default = (string)"responses")]
        public string? ResponseQueueName { get; set; }

        [Option('c', "count", Default = (int)1, HelpText = "Number of messages to send and receive")]
        public int Count { get; set; }

        [Option('b', "batch-size", Default = (int)1, HelpText = "Max number of messages to receive in one receive call")]
        public int BatchSize { get; set; }

        public override void Validate()
        {
            base.Validate();
            if (string.IsNullOrWhiteSpace(RequestQueueName))
            {
                throw new ArgumentException("RequestQueueName cannot be empty!");
            }
            if (string.IsNullOrWhiteSpace(ResponseQueueName))
            {
                throw new ArgumentException("ResponseQueueName cannot be empty!");
            }
            if (Count <= 0)
            {
                throw new ArgumentException("Count cannot be less than 1!");
            }
            if (BatchSize <= 0)
            {
                throw new ArgumentException("BatchSize cannot be less than 1!");
            }
        }
    }

    static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<Options>(args)
            .MapResult(RunAsync, _ => Task.FromResult(1)).ConfigureAwait(false);
    }

    static IMessageQueue CreateSender(Options options)
    {
        var queueOpts = new CloudWorker.E2E.QueueOptions(options) { QueueName = options.RequestQueueName };
        return QueueClient.Create(queueOpts);
    }

    static IMessageQueue CreateReceiver(Options options)
    {
        var queueOpts = new CloudWorker.E2E.QueueOptions(options) { QueueName = options.ResponseQueueName };
        return QueueClient.Create(queueOpts);
    }

    static async Task<int> RunAsync(Options options)
    {
        options.Validate();

        var sender = CreateSender(options);
        var receiver = CreateReceiver(options);

        var gMethod = Greeter.Descriptor.FindMethodByName("SayHello");
        var gMsg = new HelloRequest() { Name = "Rob" };
        var request = new Request(gMethod, gMsg);

        if (options.Count == 1)
        {
            Console.WriteLine($"Send '{gMsg}' to '{request.ServiceName}::{request.MethodName}' with request id {request.Id}.");
            await sender.SendGRpcMessageAsync(request);

            var response = await receiver.WaitGRpcMessageAsync<HelloReply>();
            Console.WriteLine($"Received '{response.GRpcMessage}' in reply to {response.InReplyTo}.");

            await response.QueueMessage!.DeleteAsync();
            Console.WriteLine($"Deleted the message.");
        }
        else
        {
            var sendTasks = new Task[options.Count];
            for (var i = 0; i < options.Count; i++)
            {
                sendTasks[i] = sender.SendGRpcMessageAsync(request);
            }

            //It's optional to wait all the sending tasks
            await Task.WhenAll(sendTasks);

            var deleteTasks = new List<Task>();
            while (deleteTasks.Count < sendTasks.Length)
            {
                var responses = await receiver.WaitGRpcMessagesAsync<HelloReply>(options.BatchSize);
                var errorCount = responses.Where(resp => resp.Error != null).Count();
                Console.WriteLine($"Received {responses.Count} messages, in which there're {errorCount} errors.");

                deleteTasks.AddRange(responses.Select(resp => resp.QueueMessage!.DeleteAsync()));
            }
            await Task.WhenAll(deleteTasks);
            Console.WriteLine($"Deleted {deleteTasks.Count} messages.");
        }

        return 0;
    }
}
