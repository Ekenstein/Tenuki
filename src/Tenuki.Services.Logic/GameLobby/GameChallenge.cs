using Tenuki.Commons;

namespace Tenuki.Services.Logic.GameLobby;

public sealed record GameChallenge(ChallengeId Id, GameLobbySettings ProposedSettings);
