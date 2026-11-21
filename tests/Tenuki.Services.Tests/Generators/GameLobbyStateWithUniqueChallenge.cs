using Tenuki.Commons;
using Tenuki.Services.Logic.GameLobby;

namespace Tenuki.Services.Tests.Generators;

public sealed record GameLobbyStateWithUniqueChallenge(
    GameLobbyState.Waiting Lobby,
    GameChallenge Challenge
);
