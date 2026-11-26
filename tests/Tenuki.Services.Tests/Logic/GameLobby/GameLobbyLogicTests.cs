using FsCheck.Xunit;
using Tenuki.Commons;
using Tenuki.Services.Logic.GameLobby;
using Tenuki.Services.Tests.Generators;

namespace Tenuki.Services.Tests.Logic.GameLobby;

[Properties(Arbitrary = [typeof(Arbitraries)])]
public sealed class GameLobbyLogicTests
{
    [Property]
    public bool Add_Challenge_Adds_The_New_Challenge_To_The_Lobby(
        GameLobbyStateWithUniqueChallenge lobbyAndChallenge
    )
    {
        var (lobby, challenge) = lobbyAndChallenge;
        var result = GameLobbyLogic.AddChallenge(lobby, challenge);
        return result.TryGetValue(out var updatedLobby)
            && updatedLobby.Challenges.SequenceEqual(lobby.Challenges.Add(challenge))
            && updatedLobby.GameId == lobby.GameId
            && updatedLobby.Settings == lobby.Settings;
    }

    [Property]
    public bool Add_Challenge_Rejects_Duplicate_Id(
        GameLobbyStateWithDuplicateChallenge lobbyAndChallenge
    )
    {
        var (lobby, challenge) = lobbyAndChallenge;
        var result = GameLobbyLogic.AddChallenge(lobby, challenge);
        return !result.IsSuccess
            && result.Errors.Contains(GameLobbyLogicErrors.ChallengeAlreadyExist);
    }

    [Property]
    public bool Add_Challenge_Rejects_Existing_Challenge(
        GameLobbyStateWithExistingChallenge lobbyAndChallenge
    )
    {
        var (lobby, challenge) = lobbyAndChallenge;
        var result = GameLobbyLogic.AddChallenge(lobby, challenge);

        return !result.IsSuccess
            && result.Errors.Contains(GameLobbyLogicErrors.ChallengeAlreadyExist);
    }

    [Property]
    public bool Reject_Challenge_Only_Removes_The_Corresponding_Challenge(
        GameLobbyStateWithExistingChallenge lobbyAndChallenge
    )
    {
        var (lobby, challenge) = lobbyAndChallenge;
        var result = GameLobbyLogic.RemoveChallenge(lobby, challenge.Id);
        return result.TryGetValue(out var updatedLobby)
            && updatedLobby.Challenges.SequenceEqual(
                lobby.Challenges.Where(x => x.Id != challenge.Id)
            )
            && updatedLobby.GameId == lobby.GameId
            && updatedLobby.Settings == lobby.Settings;
    }

    [Property]
    public bool Reject_Challenge_Fails_If_Challenge_Does_Not_Exist(
        GameLobbyStateWithUniqueChallenge lobbyAndChallenge
    )
    {
        var (lobby, challenge) = lobbyAndChallenge;
        var result = GameLobbyLogic.RemoveChallenge(lobby, challenge.Id);
        return !result.IsSuccess && result.Errors.Contains(GameLobbyLogicErrors.ChallengeNotFound);
    }

    [Property]
    public bool Accept_Challenge_Fails_If_Challenge_Does_Not_Exist(
        GameLobbyStateWithUniqueChallenge lobbyAndChallenge
    )
    {
        var (lobby, challenge) = lobbyAndChallenge;
        var result = GameLobbyLogic.AcceptChallenge(lobby, challenge.Id);
        return !result.IsSuccess && result.Errors.Contains(GameLobbyLogicErrors.ChallengeNotFound);
    }

    [Property]
    public bool Accept_Challenge_Starts_Game_With_Proposed_Settings(
        GameLobbyStateWithExistingChallenge lobbyAndChallenge
    )
    {
        var (lobby, challenge) = lobbyAndChallenge;
        var result = GameLobbyLogic.AcceptChallenge(lobby, challenge.Id);
        return result.TryGetValue(out var updatedLobby)
            && updatedLobby.AcceptedChallengeId == challenge.Id
            && updatedLobby.GameId == lobby.GameId
            && updatedLobby.Settings == challenge.ProposedSettings;
    }

    [Property]
    public bool Add_Challenge_Fails_When_Game_Is_Started(
        GameLobbyState.Started lobby,
        GameChallenge challenge
    )
    {
        var result = GameLobbyLogic.AddChallenge(lobby, challenge);
        return !result.IsSuccess && result.Errors.Contains(GameLobbyLogicErrors.GameAlreadyStarted);
    }

    [Property]
    public bool Reject_Challenge_Fails_When_Game_Is_Started(
        GameLobbyState.Started lobby,
        GameChallenge challenge
    )
    {
        var result = GameLobbyLogic.RemoveChallenge(lobby, challenge.Id);
        return !result.IsSuccess && result.Errors.Contains(GameLobbyLogicErrors.GameAlreadyStarted);
    }

    [Property]
    public bool Accept_Challenge_Fails_When_Game_Is_Started(
        GameLobbyState.Started lobby,
        GameChallenge challenge
    )
    {
        var result = GameLobbyLogic.AcceptChallenge(lobby, challenge.Id);
        return !result.IsSuccess && result.Errors.Contains(GameLobbyLogicErrors.GameAlreadyStarted);
    }
}
