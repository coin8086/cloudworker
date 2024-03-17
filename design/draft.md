# Cloud Native SOA

## Overview

Cloud SOA is a queue based SOA system on Azure.

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

**Create a session**

Queues and cluster are created externally to a session.

```cs
var requestQueue = await Queue.CreateAsync(...);
var responseQueue = await Queue.CreateAsync(...);
var cluster = await Cluster.CreateAsync(...);
var session = await Session.CreateAsync(requestQueue, responseQueue);
```

Question: do we really need a session, since the request and response queues are enough for sending requests and receiving responses?

**Send requests by the session**

```cs
var tasks = new Task[1000];
for (var i = 1; i < 1000; i++) {
    tasks[i] = session.sendRequestAsync(...);
}
await Task.WhenAll(tasks);
```

**Get responses by the session**

```cs
for (var i = 1; i < 1000; i++) {
    var result = await session.WaitResponseAsync()

    //Process the result...

    //Delete the result message finally
    await result.DeleteAsync();
}
```
