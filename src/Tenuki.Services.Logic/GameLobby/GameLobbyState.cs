using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Services.Logic.GameLobby;

public closed record GameLobbyState(GameId GameId, GameSettings GameSettings)
{
    public sealed record Waiting(
        GameId GameId,
        GameSettings GameSettings,
        ImmutableList<GameChallenge> Challenges
    ) : GameLobbyState(GameId, GameSettings);

    public sealed record Started(
        GameId GameId,
        GameSettings GameSettings,
        ColorPreference ColorPreference,
        ChallengeId AcceptedChallengeId
    ) : GameLobbyState(GameId, GameSettings);
}
