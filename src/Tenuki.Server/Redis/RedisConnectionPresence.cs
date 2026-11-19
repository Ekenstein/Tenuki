using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Tenuki.Server.Options;
using Tenuki.Services.Application;

namespace Tenuki.Server.Redis;

internal sealed class RedisConnectionPresence(
    IConnectionMultiplexer redis,
    IOptions<ConnectionPresenceOptions> options
) : IConnectionPresence
{
    private readonly TimeSpan ConnectionTtl = TimeSpan.FromMilliseconds(
        options.Value.Ttl.LifetimeInMs
    );

    private static RedisKey ConnectionKey(string connectionId) =>
        $"tenuki:presence:connection:{connectionId}";

    public Task<bool> IsActiveAsync(string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = redis.GetDatabase();
        return database.KeyExistsAsync(ConnectionKey(connectionId));
    }

    public Task RefreshAsync(string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return RegisterAsync(connectionId, cancellationToken);
    }

    public Task RegisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = redis.GetDatabase();
        return database.StringSetAsync(ConnectionKey(connectionId), "1", expiry: ConnectionTtl);
    }

    public Task<bool> UnregisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = redis.GetDatabase();
        return database.KeyDeleteAsync(ConnectionKey(connectionId));
    }
}
