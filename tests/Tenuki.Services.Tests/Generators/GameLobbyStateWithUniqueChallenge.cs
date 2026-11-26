using Tenuki.Commons;

namespace Tenuki.Services.Tests.Generators;

public sealed record GameLobbyStateWithUniqueChallenge(
    GameLobbyState.Waiting Lobby,
    GameChallenge Challenge
);
