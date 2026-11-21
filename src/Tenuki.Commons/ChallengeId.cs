namespace Tenuki.Commons;

public sealed record ChallengeId(Guid Value) : Id<ChallengeId>(Value);
