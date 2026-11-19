using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Tenuki.Commons;

namespace Tenuki.Protocol;

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record GameMessage(GameId GameId)
{
    public sealed record StateUpdated(
        GameId GameId, 
        string StateToken,
        long PreviousRevision,
        long Revision, 
        ImmutableList<GameCommand> CommittedCommands
    ) : GameMessage(GameId);
}
