using CloudWorker.MessageQueue;
using Google.Protobuf;
using System.Collections.Immutable;

namespace CloudWorker.Services.GRpcAdapter.Client;

public static class IMessageQueueExtensions
{
    public static Task SendGRpcMessageAsync(this IMessageQueue queue, Request request, CancellationToken cancel = default)
    {
        return queue.SendAsync(request.ToJson(), cancel);
    }

    public static async Task<IReadOnlyList<Response<T>>> WaitGRpcMessagesAsync<T>(this IMessageQueue queue, int batchSize, CancellationToken cancel = default)
        where T : IMessage<T>, new()
    {
        var results = await queue.WaitBatchAsync(batchSize, cancel);
        return results.Select(qMsg => new Response<T>(qMsg)).ToImmutableList();
    }

    public static async Task<Response<T>> WaitGRpcMessageAsync<T>(this IMessageQueue queue, CancellationToken cancel = default)
        where T : IMessage<T>, new()
    {
        var results = await queue.WaitGRpcMessagesAsync<T>(1, cancel);
        return results[0];
    }
}
