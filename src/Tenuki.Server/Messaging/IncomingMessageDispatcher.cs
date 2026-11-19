using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Tenuki.Protocol;

namespace Tenuki.Server.Messaging;

public sealed class IncomingMessageDispatcher(
    IMessageBroadcaster<LobbyMessage> lobby,
    IMessageBroadcaster<GameMessage> games
)
{
    public async IAsyncEnumerable<ImmutableList<TId>> DispatchAsync<TId>(
        IEnumerable<ReceivedMessage<TId, IncomingMessage>> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
        where TId : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outgoingMessages = messages.Batch();

        foreach (var outgoingMessage in outgoingMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var broadcast = outgoingMessage switch
            {
                OutgoingMessage<TId>.Game x => games.BroadcastAsync(x.Message, cancellationToken),
                OutgoingMessage<TId>.Lobby x => lobby.BroadcastAsync(x.Message, cancellationToken),
            };

            await broadcast;
            yield return outgoingMessage.SourceIds;
        }
    }
}
