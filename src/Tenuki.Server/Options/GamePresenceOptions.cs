namespace Tenuki.Server.Options;

public sealed record GamePresenceOptions
{
    public TimedBackgroundServiceOptions BackgroundService { get; init; } =
        new() { DelayInMs = 1000L };
}
