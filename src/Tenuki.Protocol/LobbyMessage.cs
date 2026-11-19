using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Tenuki.Commons;

namespace Tenuki.Protocol;

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record LobbyMessage
{
    public sealed record GamesAdded(ImmutableList<GameDescription> Games) : LobbyMessage();

    public sealed record GamesRemoved(ImmutableList<GameId> GameIds) : LobbyMessage();
}
