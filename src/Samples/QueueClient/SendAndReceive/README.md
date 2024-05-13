# Send and Receive Messages

The sample shows how to send and receive messages by queue client `IMessageQueue`. It assumes a system of CloudWorker has already be setup.

To run the program, a queue connection string is required. You can set it by environment variable `QUEUE_CONNECTION_STRING` or in command line parameter `--connect`.

Besides, the following queue parameters are set to default values:

* `--queue-type` Valid values are "servicebus" or "storage", for Service Bus queue and Storage queue separately. The default is "servicebus".
* `--request-queue` Request queue name, default to "requests".
* `--response-queue` Response queue name, default to "responses".

Change them to match your values.

By default, the program assumes a system for Echo service has been setup. Then you can run it like

```bash
dotnet run -- --message hello --count 10
```

This will be send 10 messages of "hello".

The program can also send messages to gRPC service, with parameter `--grpc`, like

```bash
dotnet run -- --message Rob --count 10 --grpc
```

This requires a system for gRPC service [`GRpcHello`](../../../Services/GRpc/GRpcHello/). See the session sample's [configuration](../../SDK/Session/README.md##configuration) for how to setup such a system.