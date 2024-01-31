

interface IWorker
{
    async Task<JsonObject> InvokeAsync(JsonObject input)
}

//---

//Load user assembly for an implementation of IWorker
//
//Create an instance of IWorker, say worker

while (true)
{
    //1. Take a message from request queue

    //2. Start a timer to renew the lease of the message in another thread, periodically

    //3. Call InvokeAsync on worker

    //4. Put the result in response queue

    //5. Delete the request message
}

//Problem 1: That's only for C#. What about other languages?
//
//The answer could be: some IPC call like WCF, gRPC or CGI? Or make the above code in COM and make a wrapper in different languages with callbacks just like Symphony does? Or ...?
//
//Problme 2: When to end? Is it busy polling the queue endlessly?
//
//The answer could be a kind of "ending message", which is also a kind of "broadcast" message. But the storage queue can't ensure the message order in a queue?
