using System.Collections.Concurrent;
using System.Collections.Immutable;
using Tenuki.Services.Application;

namespace Tenuki.Server;

public sealed class ConnectionRegistry(IConnectionPresence presence)
{
    private readonly ConcurrentDictionary<string, string> _connections = new();

    public async Task AddAsync(string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await presence.RegisterAsync(connectionId, cancellationToken);
        _connections.TryAdd(connectionId, connectionId);
    }

    public async Task RemoveAsync(string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await presence.UnregisterAsync(connectionId, cancellationToken);
        _connections.TryRemove(connectionId, out _);
    }

    public IImmutableSet<string> GetAllConnections()
    {
        return _connections.Keys.ToImmutableHashSet();
    }
}
