using System.Collections.Immutable;
using Tenuki.Commons;
using Tenuki.Services.Logic.Game;

namespace Tenuki.Services.Logic.GameLobby;

public static class GameLobbyLogic
{
    public static Result<GameLobbyState.Waiting> CreateLobby(
        GameId gameId,
        GameLobbySettings settings
    )
    {
        var errors = settings.GameSettings.Validate().ToImmutableList();
        if (errors.Count > 0)
        {
            return new Failure(errors);
        }

        var lobby = new GameLobbyState.Waiting(gameId, settings, [], 1);
        return Result.Success(lobby);
    }

    public static Result<GameLobbyState.Waiting> AddChallenge(
        GameLobbyState state,
        GameChallenge challenge
    ) =>
        state switch
        {
            GameLobbyState.Waiting waiting => AddChallenge(waiting, challenge),
            GameLobbyState.Started => new Failure([GameLobbyLogicErrors.GameAlreadyStarted]),
        };

    private static Result<GameLobbyState.Waiting> AddChallenge(
        GameLobbyState.Waiting state,
        GameChallenge challenge
    )
    {
        if (state.Challenges.Any(x => x.Id == challenge.Id))
        {
            return new Failure([GameLobbyLogicErrors.ChallengeAlreadyExist]);
        }

        var errors = challenge.ProposedSettings.GameSettings.Validate().ToImmutableList();
        if (errors.Count > 0)
        {
            return new Failure(errors);
        }

        var updatedState = state with
        {
            Challenges = state.Challenges.Add(challenge),
            Revision = state.Revision + 1,
        };

        return Result.Success(updatedState);
    }

    public static Result<GameLobbyState.Waiting> RemoveChallenge(
        GameLobbyState state,
        ChallengeId challengeId
    ) =>
        state switch
        {
            GameLobbyState.Waiting waiting => RemoveChallenge(waiting, challengeId),
            GameLobbyState.Started => new Failure([GameLobbyLogicErrors.GameAlreadyStarted]),
        };

    private static Result<GameLobbyState.Waiting> RemoveChallenge(
        GameLobbyState.Waiting state,
        ChallengeId challengeId
    )
    {
        var updatedState = state with
        {
            Challenges = state.Challenges.RemoveAll(x => x.Id == challengeId),
            Revision = state.Revision + 1,
        };

        if (updatedState.Challenges.Count == state.Challenges.Count)
        {
            return new Failure([GameLobbyLogicErrors.ChallengeNotFound]);
        }

        return Result.Success(updatedState);
    }

    public static Result<GameLobbyState.Started> AcceptChallenge(
        GameLobbyState state,
        ChallengeId challengeId
    ) =>
        state switch
        {
            GameLobbyState.Waiting waiting => AcceptChallenge(waiting, challengeId),
            GameLobbyState.Started => new Failure([GameLobbyLogicErrors.GameAlreadyStarted]),
        };

    private static Result<GameLobbyState.Started> AcceptChallenge(
        GameLobbyState.Waiting state,
        ChallengeId challengeId
    )
    {
        var challenge = state.Challenges.SingleOrDefault(x => x.Id == challengeId);
        if (challenge == null)
        {
            return new Failure([GameLobbyLogicErrors.ChallengeNotFound]);
        }

        return Result.Success(
            new GameLobbyState.Started(
                state.GameId,
                challenge.ProposedSettings,
                challengeId,
                state.Revision + 1
            )
        );
    }
}
