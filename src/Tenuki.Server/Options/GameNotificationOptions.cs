namespace Tenuki.Server.Options;

public sealed record GameNotificationOptions
{
    public TimedBackgroundServiceOptions BackgroundService { get; init; } =
        new() { DelayInMs = 50L };
}
