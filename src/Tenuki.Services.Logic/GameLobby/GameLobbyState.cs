using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Tenuki.Commons;

namespace Tenuki.Services.Logic.GameLobby;

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record GameLobbyState(GameId GameId, GameLobbySettings Settings, long Revision)
{
    public sealed record Waiting(
        GameId GameId,
        GameLobbySettings Settings,
        ImmutableList<GameChallenge> Challenges,
        long Revision
    ) : GameLobbyState(GameId, Settings, Revision);

    public sealed record Started(
        GameId GameId,
        GameLobbySettings Settings,
        ChallengeId AcceptedChallengeId,
        long Revision
    ) : GameLobbyState(GameId, Settings, Revision);
}
