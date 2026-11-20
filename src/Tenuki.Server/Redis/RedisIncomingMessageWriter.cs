using System.Text.Json;
using StackExchange.Redis;
using Tenuki.Server.Messaging;

namespace Tenuki.Server.Redis;

public static class RedisIncomingMessageWriter
{
    public static Task<RedisValue> QueueAsync(
        this IDatabase database,
        IncomingMessage message,
        RedisStreamSettings streamSettings,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return database.StreamAddAsync(
            streamSettings.StreamKey,
            SerializeIncomingMessage(message, jsonOptions)
        );
    }

    public static Task<RedisValue> QueueAsync(
        this ITransaction transaction,
        IncomingMessage message,
        RedisStreamSettings streamSettings,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return transaction.StreamAddAsync(
            streamSettings.StreamKey,
            SerializeIncomingMessage(message, jsonOptions)
        );
    }

    private static NameValueEntry[] SerializeIncomingMessage(
        IncomingMessage message,
        JsonSerializerOptions jsonOptions
    )
    {
        var envelope = new IncomingMessageEnvelope(message);
        return [.. envelope.ToEntries(jsonOptions)];
    }
}
