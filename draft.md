Basically, user needs to provide an implementation for

```cs
interface IWorker
{
    async Task<JsonObject> InvokeAsync(JsonObject input)
}
```

In the Service Host:

```cs
//Load user assembly for an implementation of IWorker
//And create an instance of IWorker, say worker

while (true)
{
    //1. Take a message from request queue

    //2. Start a timer to renew the lease of the message in another thread, periodically

    //3. Call InvokeAsync on worker

    //4. Put the result in response queue

    //5. Delete the request message and stop the lease timer
}
```

Problems:

1. That's only for C#. What about other languages?

   The answer could be:

     * Some IPC call like WCF, gRPC or CGI?
     * ~~Or make the above code in COM and make wrappers in different languages with callbacks just like Symphony does?~~
     * Or ...?

   We could do some dynamic loading of an assembly, which can support both in-process integration with user code just like linking a library, and out-of-process integration with user code in gRPC, WCF, or CGI, etc.

2. When to end? Is it busy polling the queue endlessly?

   The answer could be a kind of "ending message", which is also a kind of "broadcast" message. The ending message should be the last message sent to the queue. However, note that in Storage Queue the message order is not ensured and a message may be enqueued more than once.
