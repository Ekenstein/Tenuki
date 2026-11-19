using Microsoft.Extensions.Options;
using Tenuki.Server.Options;

namespace Tenuki.Server.BackgroundServices;

internal sealed class ConnectionPresenceBackgroundService(
    ConnectionPresenceRefresher refresher,
    TimeProvider timeProvider,
    IOptions<ConnectionPresenceOptions> options
) : TimedBackgroundService(options.Value.BackgroundService, timeProvider)
{
    protected override Task ExecuteTickAsync(CancellationToken stoppingToken) =>
        refresher.RefreshAllConnectionsAsync(stoppingToken);
}
