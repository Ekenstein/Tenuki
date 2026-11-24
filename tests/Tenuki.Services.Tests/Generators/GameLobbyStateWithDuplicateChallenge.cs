using Tenuki.Services.Logic.GameLobby;

namespace Tenuki.Services.Tests.Generators;

public sealed record GameLobbyStateWithDuplicateChallenge(
    GameLobbyState.Waiting Lobby,
    GameChallenge Challenge
);
