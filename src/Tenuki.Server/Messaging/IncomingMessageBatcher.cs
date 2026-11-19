using Tenuki.Protocol;

namespace Tenuki.Server.Messaging;

public static class IncomingMessageBatcher
{
    public static IEnumerable<OutgoingMessage<TId>> Batch<TId>(
        this IEnumerable<ReceivedMessage<TId, IncomingMessage>> receivedMessages
    )
    {
        var batches = receivedMessages.GroupAdjacentBy(
            (a, b) => a.Message.GetType() == b.Message.GetType()
        );

        foreach (var batch in batches)
        {
            var outgoingMessages = batch[0].Message switch
            {
                IncomingMessage.GameAdded _ => BatchAddedGames(
                    batch.Select(x => x.Narrow<IncomingMessage.GameAdded>())
                ),
                IncomingMessage.GameRemoved _ => BatchRemovedGames(
                    batch.Select(x => x.Narrow<IncomingMessage.GameRemoved>())
                ),
                IncomingMessage.GameChanged _ => BatchChangedGames(
                    batch.Select(x => x.Narrow<IncomingMessage.GameChanged>())
                ),
            };

            foreach (var outgoingMessage in outgoingMessages)
            {
                yield return outgoingMessage;
            }
        }
    }

    private static IEnumerable<OutgoingMessage<TId>> BatchAddedGames<TId>(
        IEnumerable<ReceivedMessage<TId, IncomingMessage.GameAdded>> addedGames
    )
    {
        yield return new OutgoingMessage<TId>.Lobby(
            new LobbyMessage.GamesAdded([.. addedGames.Select(x => x.Message.Game)]),
            [.. addedGames.Select(x => x.Id)]
        );
    }

    private static IEnumerable<OutgoingMessage<TId>> BatchRemovedGames<TId>(
        IEnumerable<ReceivedMessage<TId, IncomingMessage.GameRemoved>> removedGames
    )
    {
        yield return new OutgoingMessage<TId>.Lobby(
            new LobbyMessage.GamesRemoved([.. removedGames.Select(x => x.Message.GameId)]),
            [.. removedGames.Select(x => x.Id)]
        );
    }

    private static IEnumerable<OutgoingMessage<TId>> BatchChangedGames<TId>(
        IEnumerable<ReceivedMessage<TId, IncomingMessage.GameChanged>> changedGames
    )
    {
        foreach (var gameChangedById in changedGames.GroupBy(x => x.Message.Game.Id))
        {
            var orderedByRevision = gameChangedById.OrderBy(x => x.Message.Game.Revision);
            var batches = orderedByRevision.GroupAdjacentBy(
                (a, b) => a.Message.Game.Revision + 1 == b.Message.Game.Revision
            );

            foreach (var batch in batches)
            {
                var first = batch[0];
                var latest = batch[^1];
                var allCommands = batch.SelectMany(x => x.Message.CommittedCommands);

                var gameMessage = new GameMessage.StateUpdated(
                    GameId: gameChangedById.Key,
                    StateToken: latest.Message.Game.StateToken,
                    PreviousRevision: first.Message.Game.Revision - 1,
                    Revision: latest.Message.Game.Revision,
                    CommittedCommands: [.. allCommands]
                );

                yield return new OutgoingMessage<TId>.Game(
                    gameMessage,
                    [.. batch.Select(x => x.Id)]
                );
            }
        }
    }
}
