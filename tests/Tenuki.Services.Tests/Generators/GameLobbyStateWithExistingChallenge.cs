using Tenuki.Commons;

namespace Tenuki.Services.Tests.Generators;

public sealed record GameLobbyStateWithExistingChallenge(
    GameLobbyState.Waiting State,
    GameChallenge Challenge
);
