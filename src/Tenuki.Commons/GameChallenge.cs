namespace Tenuki.Commons;

public sealed record GameChallenge(
    ChallengeId Id,
    GameSettings ProposedSettings,
    ColorPreference ColorPreference
);
