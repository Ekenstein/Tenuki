using FsCheck.Xunit;
using Moq;
using Tenuki.Protocol;
using Tenuki.Server.Messaging;
using Xunit;

namespace Tenuki.Server.Tests.Messaging;

public sealed class IncomingMessageDispatcherTests
{
    [Fact]
    public async Task Empty_Input_Does_Not_Broadcast()
    {
        var lobby = new Mock<IMessageBroadcaster<LobbyMessage>>();
        var games = new Mock<IMessageBroadcaster<GameMessage>>();
        var dispatcher = new IncomingMessageDispatcher(lobby.Object, games.Object);

        await dispatcher
            .DispatchAsync<Guid>([], TestContext.Current.CancellationToken)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        lobby.Verify(
            x => x.BroadcastAsync(It.IsAny<LobbyMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        games.Verify(
            x => x.BroadcastAsync(It.IsAny<GameMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Property(Arbitrary = [typeof(Generators)])]
    public async Task SourceIds_Are_Yielded_Only_After_Broadcast_Completes(
        ReceivedMessage<Guid, IncomingMessage> message
    )
    {
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        var lobby = new Mock<IMessageBroadcaster<LobbyMessage>>().Apply(x =>
        {
            x.Setup(x =>
                    x.BroadcastAsync(
                        It.IsAny<LobbyMessage>(),
                        TestContext.Current.CancellationToken
                    )
                )
                .Returns(completion.Task);
        });
        var games = new Mock<IMessageBroadcaster<GameMessage>>().Apply(x =>
        {
            x.Setup(x =>
                    x.BroadcastAsync(It.IsAny<GameMessage>(), TestContext.Current.CancellationToken)
                )
                .Returns(completion.Task);
        });
        var dispatcher = new IncomingMessageDispatcher(lobby.Object, games.Object);

        await using var enumerator = dispatcher
            .DispatchAsync([message], TestContext.Current.CancellationToken)
            .GetAsyncEnumerator();

        var moveNext = enumerator.MoveNextAsync().AsTask();

        // Dispatchern väntar fortfarande på broadcast.
        Assert.False(moveNext.IsCompleted);

        completion.SetResult();

        Assert.True(await moveNext);
        Assert.Contains(message.Id, enumerator.Current);
    }
}
