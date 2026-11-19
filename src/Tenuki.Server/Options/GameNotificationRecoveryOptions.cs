namespace Tenuki.Server.Options;

public sealed record GameNotificationRecoveryOptions
{
    public long MinIdleTimeInMs { get; init; } = 60_000L;
    public int MaxNumberOfIterations { get; init; } = 10;
    public int BatchSize { get; init; } = 100;
    public TimedBackgroundServiceOptions BackgroundService { get; init; } =
        new() { DelayInMs = 15_000L };
}
