using Tenuki.Commons;

namespace Tenuki.Server.Http.Models;

public sealed record PostGameChallengeResponse(ChallengeId ChallengeId, string OwnerToken);
