namespace Tenuki.Server.Options;

public sealed record TimedBackgroundServiceOptions
{
    public long DelayInMs { get; init; } = 1000L;
}
