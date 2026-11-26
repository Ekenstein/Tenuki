using Tenuki.Commons;

namespace Tenuki.Services.Tests.Generators;

public sealed record GameLobbyStateWithDuplicateChallenge(
    GameLobbyState.Waiting Lobby,
    GameChallenge Challenge
);
