Basically, user needs to provide an implementation for

```cs
interface IService
{
    async Task<string> InvokeAsync(string json)
}
```

In the Service Host:

```cs
//Load user assembly for an implementation of IService
//And create an instance of IService, say service

while (true)
{
    //1. Take a message from request queue

    //2. Start a timer to renew the lease of the message in another thread, periodically

    //3. Call InvokeAsync on service and wait for result

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

   We could do dynamic loading of an assembly, which can support both in-process integration with user code, and out-of-process integration in gRPC, WCF, or CGI, etc.

2. When to end? Is it busy polling the queue endlessly?

   The answer could be a kind of "ending message", which is also a kind of "broadcast" message. The ending message should be the last message sent to the queue. However, note that in Storage Queue the message order is not ensured and that a message may be enqueued more than once.


In the client side:

```cs
var storageAccount = ...;
var account = await SimpleSoa.Account.CreateAsync(storageAccount);

//The primary goal of a job config is to specify a servie assembly file: where to get it and where to put it on a service host. A config also specifies data volumes and where to mount them on a service host.
var jobConfig = new SimpleSoa.JobConfig(...);

//A job consists of a request queue, a response queue and a file share in a storage account.
//Each job has a unique ID, and optionally other fields like created_at, started_at, completed_at, etc..
var job = await SimpleSoa.Job.CreateAsync(account, jobConfig);

//Then you can get the queue and volume info of a job. You need to provide them to service hosts for your BYO cluster. For a HOBO cluster, do it like the following.

//Create a cluster for the job with 100 nodes. The method returns when cluster provisioning begins.
var cluster = await SimpleSoa.Cluster.CreateAsync(job, 100);

//You can query the cluster status by
//await cluster.GetStatusAsync();

//While the cluster is creating, you can submit tasks to the job now!
for (var i = 1; i < 10000; i++) {
    //Alternatively, you can collect all tasks into an array and wait for them all at once
    //to get much more throughput.
    await job.AddTaskAsync(taskInJsonString);
}

//Completing a job means marking the end of a request queue, so that service hosts won't try to get
//more tasks from the queue. Service hosts will shutdown themselves in the end.
//After completing you can not add task the the job any more. You must call CompleteAsync when no new
//tasks are to be added, otherwise the cluser will keep busy polling the queue.
await job.CompleteAsync();

//A job's status can be created, [started,] and completed. And optionally it can include numbers of pending tasks and results, and time stamps at created, started and completed.
//You can query the job status by
//await job.GetStatusAsync();

//Get job task results by
while (true) {
    var taskResult = await job.GetTaskResult();
    if (!taskResult) {
        await job.GetStatusAsync();
        if (job.Completed) {
            break;
        }
        else {
            //The tasks are being processed and no new result for now.
            //So wait some time.
            Sleep(2000);
        }
    }
    else {
        //Process the result...
    }
}
```

Questions:

1. Do we save job into a storage table? And what about saving tasks? Then can we accept the cost of saving in performance?

2. How about logging, diagnosis and visibility? What do we provide for user for these?

3. Last but not least, do we need to support multiple programming languages? And how to?