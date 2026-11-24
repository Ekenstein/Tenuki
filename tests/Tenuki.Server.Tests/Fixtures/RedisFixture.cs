using InMemoryEmulator.Redis;
using StackExchange.Redis;
using Xunit;

namespace Tenuki.Server.Tests.Fixtures;

public sealed class RedisFixture : IAsyncLifetime
{
    private InMemoryRedisResult? _redis;

    public IConnectionMultiplexer Connection =>
        _redis?.Multiplexer
        ?? throw new InvalidOperationException("Redis has not been initialized.");

    public ValueTask DisposeAsync() => _redis?.DisposeAsync() ?? ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        _redis = await InMemoryRedis.CreateAsync();
    }
}
