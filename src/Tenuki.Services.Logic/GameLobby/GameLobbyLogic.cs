using Tenuki.Commons;

namespace Tenuki.Services.Logic.GameLobby;

public static class GameLobbyLogic
{
    public static Result<GameLobbyState.Waiting> AddChallenge(
        GameLobbyState state,
        GameChallenge challenge
    ) =>
        state switch
        {
            GameLobbyState.Waiting waiting => AddChallenge(waiting, challenge),
            GameLobbyState.Started => new Failure([GameLobbyErrors.GameAlreadyStarted]),
        };

    private static Result<GameLobbyState.Waiting> AddChallenge(
        GameLobbyState.Waiting state,
        GameChallenge challenge
    )
    {
        if (state.Challenges.Any(x => x.Id == challenge.Id))
        {
            return new Failure([GameLobbyErrors.ChallengeAlreadyExist]);
        }

        var updatedState = state with { Challenges = state.Challenges.Add(challenge) };

        return Result.Success(updatedState);
    }

    public static Result<GameLobbyState.Waiting> RejectChallenge(
        GameLobbyState state,
        ChallengeId challengeId
    ) =>
        state switch
        {
            GameLobbyState.Waiting waiting => RejectChallenge(waiting, challengeId),
            GameLobbyState.Started => new Failure([GameLobbyErrors.GameAlreadyStarted]),
        };

    private static Result<GameLobbyState.Waiting> RejectChallenge(
        GameLobbyState.Waiting state,
        ChallengeId challengeId
    )
    {
        var updatedState = state with
        {
            Challenges = state.Challenges.RemoveAll(x => x.Id == challengeId),
        };

        if (updatedState.Challenges.Count == state.Challenges.Count)
        {
            return new Failure([GameLobbyErrors.ChallengeNotFound]);
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
            GameLobbyState.Started => new Failure([GameLobbyErrors.GameAlreadyStarted]),
        };

    private static Result<GameLobbyState.Started> AcceptChallenge(
        GameLobbyState.Waiting state,
        ChallengeId challengeId
    )
    {
        var challenge = state.Challenges.SingleOrDefault(x => x.Id == challengeId);
        if (challenge == null)
        {
            return new Failure([GameLobbyErrors.ChallengeNotFound]);
        }

        return Result.Success(
            new GameLobbyState.Started(
                state.GameId,
                challenge.ProposedSettings,
                challenge.ColorPreference,
                challengeId
            )
        );
    }
}
