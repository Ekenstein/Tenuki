using System.Collections.Immutable;
using Tenuki.Protocol;

namespace Tenuki.Server.Messaging;

public closed record OutgoingMessage<TId>(ImmutableList<TId> SourceIds)
{
    public sealed record Game(GameMessage Message, ImmutableList<TId> SourceIds)
        : OutgoingMessage<TId>(SourceIds);

    public sealed record Lobby(LobbyMessage Message, ImmutableList<TId> SourceIds)
        : OutgoingMessage<TId>(SourceIds);
}
