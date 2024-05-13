# Cluster Sample

The sample shows how to use `Cluster` class to create, update and destroy a system for default Echo service on Azure.

## Configuration

A configuration file is required to create/update a cluster. A sample configuration file `ClusterConfig.json` is provided. But you need to fill the `SubScriptionId` field with your own value.

More configurable items can be found in [ClusterConfig](../../../Client/SDK/ClusterConfig.cs) class.

## Run

Create a cluster with configuration file `ClusterConfig.json`

```bash
dotnet run -- --create --config ClusterConfig.json
```

Note a line of console output like

```
ClusterID=9fda6ae5-1210-43b9-a2da-703f0cdc253e:c49d8c24-3062-44b8-9a80-8e8164d4d340
```

Here `9fda6ae5-1210-43b9-a2da-703f0cdc253e:c49d8c24-3062-44b8-9a80-8e8164d4d340` is the cluster id.

The program also shows properties about the cluster's messaging queues, like

```
Queue:Type=servicebus
Queue:RequestQueueName=requests
Queue:ResponseQueueName=responses
Queue:ConnectionString=...
```

With these properties, you can send/receive messages with your queue client.

You can update the cluster with a different configuration file, for example updating the `Service` and `EnvironmentVariables`. Do it like

```bash
dotnet run -- --update 9fda6ae5-1210-43b9-a2da-703f0cdc253e:c49d8c24-3062-44b8-9a80-8e8164d4d340 --config ClusterConfig.json
```

Finally, you destroy the cluster

```bash
dotnet run -- --delete 9fda6ae5-1210-43b9-a2da-703f0cdc253e:c49d8c24-3062-44b8-9a80-8e8164d4d340
```
