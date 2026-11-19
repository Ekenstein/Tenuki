using Tenuki.Services.Application;

namespace Tenuki.Server;

public sealed class ConnectionPresenceRefresher(
    ConnectionRegistry connectionRegistry,
    IConnectionPresence connectionPresence
)
{
    public async Task RefreshAllConnectionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var connectionId in connectionRegistry.GetAllConnections())
        {
            await connectionPresence.RefreshAsync(connectionId, cancellationToken);
        }
    }
}
