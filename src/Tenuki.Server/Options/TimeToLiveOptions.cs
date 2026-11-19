namespace Tenuki.Server.Options;

public sealed record TimeToLiveOptions
{
    public long LifetimeInMs { get; init; } = 5 * 60 * 1000;
}
