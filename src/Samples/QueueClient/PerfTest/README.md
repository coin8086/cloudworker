# Performance Test of Sending and Receiving Messages

The program is both a performance test tool and a sample for sending and receiving messages in a high performance way.

To run the program, a queue connection string is required. You can set it by environment variable `QUEUE_CONNECTION_STRING` or in command line parameter `--connect`.

Besides, the following queue parameters are set to default values:

* `--queue-type` Valid values are "servicebus" or "storage", for Service Bus queue and Storage queue separately. The default is "servicebus".
* `--request-queue` Request queue name, default to "requests".
* `--response-queue` Response queue name, default to "responses".

Change them to match your values.

Other parameters all have proper default values, among which you may want to change

* `--count` Total number of messages to send and receive, default to 2000
* `--senders` Parallel senders, default to 10
* `--receivers` Parallel receivers, default to 100
* `--message-length` Length of each message, default to 4 bytes

Here're some example command lines

```bash
dotnet run -- --count 5000
```

This sends and receives 5000 messages.

```bash
dotnet run -- --senders 0 --receivers 20 --count 1000
```

This doesn't send but only receives 1000 messages in 20 parallel receivers.

```bash
dotnet run -- --receivers 0 --count 1000
```

This doesn't receive but only sends 1000 messages.
