using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Services.Application.GameLobby;

public sealed record GameLobbyOwnership(
    SecretTokenHash Owner,
    ImmutableDictionary<ChallengeId, SecretTokenHash> Challengers
);
