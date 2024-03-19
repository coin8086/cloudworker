# Cloud SOA

## Overview

Cloud SOA is a queue based system on Cloud for SOA workload.

```mermaid
flowchart
    subgraph Azure
        cluster((service hosts))
        inq(request queue)
        outq(response queue)
        storage[(file share)]
        cluster --> |read| inq
        cluster --> |write| outq
        cluster --> storage
    end

    client((clients))
    client --> |write| inq
    client --> |read| outq
    client --> storage
```

## Service

### Used Defined Service (UDS)

In the system, a SOA service is a Used Defined Service (UDS) that implements the following interface:

```cs
interface ISoaService
{
    Task<string> InvokeAsync(string input, CancellationToken token)
}
```

### Service Host

A service host is the one that hosts a UDS.

It works like this:

```mermaid
flowchart TD
    init[Create an instance of a UDS]

    take[Take a message from request queue]

    beforeCall[Start a timer to renew
        lease of the message periodically]

    callit[Call InvokeAsync on the UDS instance]

    put[Put result in response queue]

    afterCall[Delete the request from the queue
    and stop the lease timer]

    init --> take
    take --> beforeCall
    beforeCall --> callit
    callit --> put
    put --> afterCall
    afterCall --> take
```

## Client

### Control Plane Operations

Create a pair of queues for requests and responses separately

```cs
var requestQueue = await Queue.CreateAsync(...);
var responseQueue = await Queue.CreateAsync(...);
```

Create one or more clusters for the pair of queues

```cs
var cluster = await Clsuter.CreateAsync(requestQueue, responseQueue, ...);
```

### Data Plane Operations

Send requests to request queue

```cs
var requestQueueClient = ...;
var tasks = new Task[1000];
for (var i = 1; i < 1000; i++) {
    tasks[i] = requestQueueClient.sendAsync(...);
}
await Task.WhenAll(tasks);
```

Get responses from response queue

```cs
var responseQueueClient = ...;
for (var i = 1; i < 1000; i++) {
    var result = await responseQueueClient.WaitAsync()

    //Process the result...

    //Delete the result message finally
    await result.DeleteAsync();
}
```
