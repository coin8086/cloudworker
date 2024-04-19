using CloudWorker.E2E;
using CloudWorker.MessageQueue;
using CloudWorker.Services.GRpcAdapter.Client;
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
        var gMethod = Greeter.Descriptor.FindMethodByName("SayHello");
        var gMsg = new HelloRequest() { Name = "Rob" };
        var request = new Request(gMethod, gMsg);

        Console.WriteLine($"Send '{gMsg}' to '{request.ServiceName}::{request.MethodName}' with request id {request.Id}.");
        await sender.SendGRpcMessageAsync(request);

        var receiver = CreateReceiver(options);
        var response = await receiver.WaitGRpcMessageAsync<HelloReply>();
        Console.WriteLine($"Received '{response.GRpcMessage}' in reply to {response.InReplyTo}.");

        await response.QueueMessage!.DeleteAsync();
        Console.WriteLine($"Deleted the message.");

        return 0;
    }
}
