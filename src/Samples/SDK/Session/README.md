# Session Sample

The sample shows how to use `Session` class to create, update and destroy a system for Echo/GRpc services on Azure, and how to send and receive messages with it.

## Configuration

There're two configuration files for the sample, `EchoSessionConfig.json` and `GRpcSessionConfig.json`, for clusters of Echo service and GRpc service separately. You need to choose one and fill its required fields (those that are empty, like `SubScriptionId`).

For `GRpcSessionConfig.json`, you also need a file share in Azure Storage for your gRPC service files. The sample uses a simple gRPC service [`GRpcHello`](../../../Services/GRpc/GRpcHello/). You need to build it (for Linux, since the default image for computing node is Linux) and upload the published files to a file share. Then fill the `StorageAccountName`, `StorageAccountKey` and `FileShareName` with your own values. Optionally, you can change `MountPath` (where your file share will be mounted on each computing node) and the environment variable `GRPC_ServerFileName` (where the gRPC server executable is).

For example, you build [`GRpcHello`](../../../Services/GRpc/GRpcHello/) with command line

```bash
dotnet publish -c Release -r linux-x64 --sc true
```

Then you upload the published files (in directory like "GRpcHello/bin/Release/net8.0/linux-x64/publish/") to directory "GRpcHello" in your file share (so that "/myfiles/GRpcHello/GRpcHello" points to your gPRC server executable).

## Run

Create a session with a configuration file `EchoSessionConfig.json` or `GRpcSessionConfig.json`, like

```bash
dotnet run -- --create --config GRpcSessionConfig.json
```

Note a line of console output like

```
Created session 9fda6ae5-1210-43b9-a2da-703f0cdc253e:c49d8c24-3062-44b8-9a80-8e8164d4d340
```

Here `9fda6ae5-1210-43b9-a2da-703f0cdc253e:c49d8c24-3062-44b8-9a80-8e8164d4d340` is the session id.

You can use the session to send (10) messages like

```bash
dotnet run -- --use 9fda6ae5-1210-43b9-a2da-703f0cdc253e:c49d8c24-3062-44b8-9a80-8e8164d4d340 --send hello --count 10
```

You can update the session with a different configuration file, for example updating the `Service`, `EnvironmentVariables`, and `FileShares`. Do it like

```bash
dotnet run -- --update 9fda6ae5-1210-43b9-a2da-703f0cdc253e:c49d8c24-3062-44b8-9a80-8e8164d4d340 --config GRpcSessionConfig.json
```

Finally, you destroy the session

```bash
dotnet run -- --delete 9fda6ae5-1210-43b9-a2da-703f0cdc253e:c49d8c24-3062-44b8-9a80-8e8164d4d340
```

## Note

In CloudWorker, a session is backed by a cluster composed of computing nodes and messaging queues, etc. A session ID is effectively a cluster ID. `Session` class is based on `Cluster` class, with additional methods to send and receive messages. And a session config is effectively a [cluster config](../../../Client/SDK/ClusterConfig.cs).
