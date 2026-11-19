namespace Tenuki.Commons;

public sealed record ClockState(PlayerClock Black, PlayerClock White, DateTimeOffset TurnStartedAt);
