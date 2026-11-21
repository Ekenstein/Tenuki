using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Tenuki.Protocol;
using Tenuki.Server.Messaging;
using Xunit;

namespace Tenuki.Server.Tests.Messaging;

[Properties(Arbitrary = [typeof(Generators)])]
public sealed class IncomingMessageBatcherTests
{
    [Fact]
    public void Empty_Input_Gives_Empty_Output()
    {
        var result = IncomingMessageBatcher.Batch<Guid>([]);
        Assert.Empty(result);
    }

    [Property]
    public bool All_Ids_Are_Preserved(MessageEnvelope<Guid, IncomingMessage>[] items)
    {
        var result = items.Batch();
        var expected = items.Select(x => x.Id).Order();
        var actual = result.SelectMany(x => x.SourceIds).Order();
        return expected.SequenceEqual(actual);
    }

    [Property]
    public bool There_Are_No_Duplications_Of_Ids(MessageEnvelope<Guid, IncomingMessage>[] items)
    {
        var result = items.Batch();
        var allIds = result.SelectMany(x => x.SourceIds);
        var uniqueIds = allIds.Distinct();
        return uniqueIds.Count() == allIds.Count();
    }

    [Property]
    public void Batch_Is_Deterministic(MessageEnvelope<Guid, IncomingMessage>[] items)
    {
        var result1 = items.Batch().ToArray();
        var result2 = items.Batch().ToArray();
        Assert.Equivalent(result1, result2, strict: true);
    }

    [Property]
    public bool Batches_Only_Contain_Messages_Of_The_Same_Type(
        MessageEnvelope<Guid, IncomingMessage>[] items
    )
    {
        var incomingById = items.ToDictionary(x => x.Id);

        return items
            .Batch()
            .All(batch =>
                batch.SourceIds.Select(id => incomingById[id].Message.GetType()).Distinct().Count()
                == 1
            );
    }

    [Property]
    public bool OutgoingMessage_Type_Represents_The_Incoming_Message_Type(
        MessageEnvelope<Guid, IncomingMessage>[] items
    )
    {
        var incomingById = items.ToDictionary(x => x.Id);
        return items
            .Batch()
            .All(batch =>
            {
                var incomingType = incomingById[batch.SourceIds[0]].Message;
                return (incomingType, batch) switch
                {
                    (
                        IncomingMessage.GameAdded,
                        OutgoingMessage<Guid>.Lobby { Message: LobbyMessage.GamesAdded }
                    ) => true,
                    (
                        IncomingMessage.GameRemoved,
                        OutgoingMessage<Guid>.Lobby { Message: LobbyMessage.GamesRemoved }
                    ) => true,
                    (
                        IncomingMessage.GameChanged,
                        OutgoingMessage<Guid>.Game { Message: GameMessage.StateUpdated }
                    ) => true,
                    (_, _) => false,
                };
            });
    }

    [Property]
    public bool StateUpdated_Accurately_Represents_Its_Source_Messages(
        MessageEnvelope<Guid, IncomingMessage.GameChanged>[] items
    )
    {
        var receivedMessages = items.Select(x =>
            MessageEnvelope.Create(x.Id, (IncomingMessage)x.Message)
        );

        return receivedMessages
            .Batch()
            .All(outgoing =>
            {
                var revisions = items
                    .Where(x => outgoing.SourceIds.Contains(x.Id))
                    .OrderBy(x => x.Message.Game.Revision)
                    .ToArray();

                var first = revisions[0].Message.Game;
                var last = revisions[^1].Message.Game;

                var revisionSequence = revisions.Zip(revisions.Skip(1));

                return outgoing
                        is OutgoingMessage<Guid>.Game { Message: GameMessage.StateUpdated message }
                    && message.PreviousRevision == first.Revision - 1
                    && message.Revision == last.Revision
                    && message.StateToken == last.StateToken
                    && revisions.All(x => x.Message.Game.Id == message.GameId)
                    && revisionSequence.All(pair =>
                        pair.First.Message.Game.Revision + 1 == pair.Second.Message.Game.Revision
                    )
                    && revisions
                        .SelectMany(x => x.Message.CommittedCommands)
                        .SequenceEqual(message.CommittedCommands);
            });
    }
}
