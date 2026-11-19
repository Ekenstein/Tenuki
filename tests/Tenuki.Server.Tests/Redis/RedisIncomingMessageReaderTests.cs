using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Tenuki.Protocol;
using Tenuki.Server.Messaging;
using Tenuki.Server.Redis;
using Tenuki.Server.Tests.Fixtures;
using Xunit;

namespace Tenuki.Server.Tests.Redis;

public sealed class RedisIncomingMessageReaderTests(RedisFixture fixture)
    : IClassFixture<RedisFixture>
{
    private static readonly IOptions<JsonOptions> JsonOptions =
        Microsoft.Extensions.Options.Options.Create(
            new JsonOptions().Apply(x => JsonUtils.ConfigureOptions(x.SerializerOptions))
        );

    [Fact]
    public async Task Pending_Message_Can_Be_Recovered_By_Another_Consumer()
    {
        var db = fixture.Connection.GetDatabase();

        var streamSettings = new RedisStreamSettings(
            StreamKey: $"tenuki:test:messages:{Guid.NewGuid():N}",
            ConsumerGroup: $"tenuki:test:group:{Guid.NewGuid():N}"
        );

        var message = new IncomingMessage.GameRemoved(new());
        var messageId = await db.QueueAsync(
            message,
            streamSettings,
            JsonUtils.DefaultOptions,
            TestContext.Current.CancellationToken
        );

        RedisValue consumerA = "consumer-A";
        RedisValue consumerB = "consumer-B";

        var lobbyBroadcaster = new Mock<IMessageBroadcaster<LobbyMessage>>();
        var gameBroadcaster = new Mock<IMessageBroadcaster<GameMessage>>();

        // First broadcast fails.
        lobbyBroadcaster
            .SetupSequence(x =>
                x.BroadcastAsync(It.IsAny<LobbyMessage>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException())
            .Returns(Task.CompletedTask);

        var dispatcher = new IncomingMessageDispatcher(
            lobbyBroadcaster.Object,
            gameBroadcaster.Object
        );

        var reader = new RedisIncomingMessageReader(
            fixture.Connection,
            dispatcher,
            streamSettings,
            JsonOptions
        );

        await reader.InitializeAsync(TestContext.Current.CancellationToken);

        // Consumer A reads the message, but dispatching fails.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.ReadIncomingMessagesAsync(consumerA, TestContext.Current.CancellationToken)
        );

        // Message remains pending.
        var pending = await db.StreamPendingMessagesAsync(
            streamSettings.StreamKey,
            streamSettings.ConsumerGroup,
            count: 10,
            consumerName: RedisValue.Null
        );

        Assert.Contains(pending, x => x.MessageId == messageId);

        // Consumer B recovers the message.
        await reader.RecoverPendingMessagesAsync(
            consumerB,
            TimeSpan.Zero,
            "0-0",
            10,
            100,
            TestContext.Current.CancellationToken
        );

        // Broadcast should have been attempted twice:
        // once by A (failed), once by B (succeeded).
        lobbyBroadcaster.Verify(
            x => x.BroadcastAsync(It.IsAny<LobbyMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );

        // Message should no longer be pending.
        pending = await db.StreamPendingMessagesAsync(
            streamSettings.StreamKey,
            streamSettings.ConsumerGroup,
            count: 10,
            consumerName: RedisValue.Null
        );

        Assert.DoesNotContain(pending, x => x.MessageId == messageId);
    }

    [Fact]
    public async Task Recovery_Continues_From_Previous_Cursor()
    {
        var db = fixture.Connection.GetDatabase();

        var lobbyBroadcaster = new Mock<IMessageBroadcaster<LobbyMessage>>();
        var gameBroadcaster = new Mock<IMessageBroadcaster<GameMessage>>();

        var dispatcher = new IncomingMessageDispatcher(
            lobbyBroadcaster.Object,
            gameBroadcaster.Object
        );

        var streamSettings = new RedisStreamSettings(
            StreamKey: $"tenuki:test:messages:{Guid.NewGuid():N}",
            ConsumerGroup: $"tenuki:test:group:{Guid.NewGuid():N}"
        );

        var reader = new RedisIncomingMessageReader(
            fixture.Connection,
            dispatcher,
            streamSettings,
            JsonOptions
        );

        await reader.InitializeAsync(TestContext.Current.CancellationToken);

        RedisValue consumerA = $"consumer-A-{Guid.NewGuid():N}";
        RedisValue consumerB = $"consumer-B-{Guid.NewGuid():N}";

        const int messageCount = 25;
        const int batchSize = 2;

        // Queue messages.
        var messageIds = new HashSet<RedisValue>();

        for (var i = 0; i < messageCount; i++)
        {
            var id = await db.QueueAsync(
                new IncomingMessage.GameRemoved(new()),
                streamSettings,
                JsonUtils.DefaultOptions,
                TestContext.Current.CancellationToken
            );

            messageIds.Add(id);
        }

        // Consumer A reads all messages without acknowledging.
        var entries = await db.StreamReadGroupAsync(
            streamSettings.StreamKey,
            streamSettings.ConsumerGroup,
            consumerA,
            StreamPosition.NewMessages,
            count: messageCount
        );

        Assert.Equal(messageCount, entries.Length);
        Assert.All(entries, x => Assert.Contains(x.Id, messageIds));

        // First recovery: one iteration only.
        var cursor = await reader.RecoverPendingMessagesAsync(
            consumerB,
            TimeSpan.Zero,
            startCursor: "0-0",
            maxNumberOfIterations: 1,
            batchSize: batchSize,
            TestContext.Current.CancellationToken
        );

        Assert.NotEqual((RedisValue)"0-0", cursor);

        var pending = await db.StreamPendingAsync(
            streamSettings.StreamKey,
            streamSettings.ConsumerGroup
        );

        Assert.True(pending.PendingMessageCount > 0);
        Assert.True(pending.PendingMessageCount < messageCount);

        // Continue from previous cursor.
        cursor = await reader.RecoverPendingMessagesAsync(
            consumerB,
            TimeSpan.Zero,
            startCursor: cursor,
            maxNumberOfIterations: 20,
            batchSize: batchSize,
            TestContext.Current.CancellationToken
        );

        Assert.Equal((RedisValue)"0-0", cursor);

        pending = await db.StreamPendingAsync(
            streamSettings.StreamKey,
            streamSettings.ConsumerGroup
        );

        Assert.Equal(0, pending.PendingMessageCount);

        lobbyBroadcaster.Verify(
            x => x.BroadcastAsync(It.IsAny<LobbyMessage>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce
        );
    }
}
