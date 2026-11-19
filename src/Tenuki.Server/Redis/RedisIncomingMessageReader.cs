using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Immutable;
using Tenuki.Server.Messaging;

namespace Tenuki.Server.Redis;

public sealed class RedisIncomingMessageReader(
    IConnectionMultiplexer redis,
    IncomingMessageDispatcher dispatcher,
    RedisStreamSettings streamSettings,
    IOptions<JsonOptions> jsonOptions
)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                streamSettings.StreamKey,
                streamSettings.ConsumerGroup,
                StreamPosition.Beginning,
                createStream: true
            );
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // the group exists, ignore.
        }
    }

    public async Task<RedisValue> RecoverPendingMessagesAsync(
        RedisValue consumerName,
        TimeSpan minIdleTime,
        RedisValue startCursor,
        int maxNumberOfIterations,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = redis.GetDatabase();
        RedisValue cursor = startCursor;

        for (var i = 0; i < maxNumberOfIterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await db.StreamAutoClaimAsync(
                streamSettings.StreamKey,
                streamSettings.ConsumerGroup,
                consumerName,
                minIdleTimeInMs: (long)minIdleTime.TotalMilliseconds,
                startAtId: cursor,
                count: batchSize
            );

            if (result.ClaimedEntries.Length > 0)
            {
                await ProcessMessagesAsync(db, result.ClaimedEntries, cancellationToken);
            }

            cursor = result.NextStartId;
            if (cursor == "0-0")
            {
                break;
            }
        }

        return cursor;
    }

    public async Task ReadIncomingMessagesAsync(
        RedisValue consumerName,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = redis.GetDatabase();
        var streamEntries = await db.StreamReadGroupAsync(
            streamSettings.StreamKey,
            streamSettings.ConsumerGroup,
            consumerName,
            StreamPosition.NewMessages,
            count: 100
        );

        await ProcessMessagesAsync(db, streamEntries, cancellationToken);
    }

    private async Task ProcessMessagesAsync(
        IDatabase db,
        StreamEntry[] streamEntries,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (errors, messages) = PartitionStreamEntries(streamEntries);
        if (errors.Count > 0)
        {
            // TODO: Log errors and move to dead-letter stream.

            // XACK so we don't get faulty messages in next read.
            await db.StreamAcknowledgeAsync(
                streamSettings.StreamKey,
                streamSettings.ConsumerGroup,
                [.. errors.Select(x => x.Id)]
            );
        }

        if (messages.Count > 0)
        {
            var dispatchedIds = new HashSet<RedisValue>();
            try
            {
                await foreach (var ids in dispatcher.DispatchAsync(messages, cancellationToken))
                {
                    dispatchedIds.UnionWith(ids);
                }
            }
            finally
            {
                if (dispatchedIds.Count > 0)
                {
                    await db.StreamAcknowledgeAsync(
                        streamSettings.StreamKey,
                        streamSettings.ConsumerGroup,
                        [.. dispatchedIds]
                    );
                }

                // for non-dispatched messages XAUTOCLAIM will be performed.
            }
        }
    }

    private (
        IReadOnlyList<ReceivedMessage<RedisValue, ImmutableList<Error>>> errors,
        IReadOnlyList<ReceivedMessage<RedisValue, IncomingMessage>> messages
    ) PartitionStreamEntries(StreamEntry[] entries)
    {
        var errors = new List<ReceivedMessage<RedisValue, ImmutableList<Error>>>();
        var messages = new List<ReceivedMessage<RedisValue, IncomingMessage>>();

        foreach (var entry in entries)
        {
            var result = IncomingMessageEnvelope
                .FromEntries(entry.Values, jsonOptions.Value.SerializerOptions)
                .Select(x => x.Message);

            switch (result)
            {
                case Success<IncomingMessage> success:
                    messages.Add(new(entry.Id, success.Value));
                    break;
                case Failure failure:
                    errors.Add(new(entry.Id, failure.Errors));
                    break;
            }
        }

        return (errors, messages);
    }
}
