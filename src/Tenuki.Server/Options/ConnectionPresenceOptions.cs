namespace Tenuki.Server.Options;

public sealed record ConnectionPresenceOptions
{
    public TimedBackgroundServiceOptions BackgroundService { get; init; } =
        new() { DelayInMs = 1000L };

    public TimeToLiveOptions Ttl { get; init; } = new() { LifetimeInMs = 2 * 60 * 1000 };
}
