using System.Text.Json.Serialization;
using Tenuki.Commons;

namespace Tenuki.Protocol;

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record GameLobbyMessage(GameId GameId)
{
    public sealed record StateUpdated(GameId GameId, long Revision, GameLobbyState State)
        : GameLobbyMessage(GameId);
}
