using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Tenuki.Commons;
using Tenuki.Protocol;

namespace Tenuki.Server.Messaging;

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record IncomingMessage
{
    public sealed record GameAdded(GameDescription Game) : IncomingMessage;

    public sealed record GameRemoved(GameId GameId) : IncomingMessage;

    public sealed record GameChanged(
        GameDescription Game,
        ImmutableList<GameCommand> CommittedCommands
    ) : IncomingMessage;
}
