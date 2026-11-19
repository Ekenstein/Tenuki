namespace Tenuki.Commons;

public sealed record PlayerClock(
    long MainTimeRemainingMs,
    int RemainingPeriods,
    long PeriodTimeRemainingMs
);
