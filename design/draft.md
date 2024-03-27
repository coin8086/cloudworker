# CloudWork

## Overview

CloudWork is a queue based system on Cloud for SOA workload.

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

### Control Plane/Cloud Resource Allocation Operations

#### Overview

Control plane/resource allocation operations can be done by tools like Azure CLI in Bash and PowerShell, etc., whatever can deploy a resource to Azure.

The main components of the system's infrastructure are:

* Queue (Service Bus queue/Storge queue/...)
* Cluster (ACI/AKS/...)
* Storage (Azure Storage file share/...)
* Monitor (Azure Monitor/...)

#### Components

Here PowerShell is used as one of the available tools for resource operations.

Create a pair of queues for requests and responses separately

```ps1
//Create a queue space with two queues, named after "requests" and "responses" separately.
$queueSpace = New-QueueSpace ...
New-Queue -Space $queueSpace -Name "requests"
New-Queue -Space $queueSpace -Name "responses"
```

Create a file share for user service and data

```ps1
$fileShare = New-FileShare ...
New-FileShareFile -LocalPath "local-user-service-package-path" -TargetPath "fileshare-path"
```

Create monitoring resources for clusters

```ps1
$monitor = New-Monitor ...
```

Create one or more clusters with the pair of queues, file share and monitor

```ps1
$queueConfig = { $queueSpace.ConnectionString, "requests", "responses", ... }
$fileshareConfig = { $fileShare.ConnectionString, "fileshare-path", "target-mount-path", ... }
$serviceConfig = { "service-assembly-path", ... }
$monitorConfig = { $monitor.ConnectionString, ... }
$clusterConfig = @{
    queue = $queueConfig
    fileShare = $fileConfig
    service = $serviceConfig
    monitor = $monitorConfig
    dockerImage = '...'
    nodes = 100
    ...
}
$cluster = New-Clsuter $clusterConfig
```

### Data Plane Operations

Data plane operations are done in a programming language like C#, linking against a SDK.

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
