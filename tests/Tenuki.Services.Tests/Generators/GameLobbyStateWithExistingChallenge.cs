using Tenuki.Commons;
using Tenuki.Services.Logic.GameLobby;

namespace Tenuki.Services.Tests.Generators;

public sealed record GameLobbyStateWithExistingChallenge(
    GameLobbyState.Waiting State,
    GameChallenge Challenge
);
