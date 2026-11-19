using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Tenuki.Server.Options;
using Tenuki.Server.Redis;

namespace Tenuki.Server.BackgroundServices;

internal sealed class GameNotificationBackgroundService(
    RedisIncomingMessageReader reader,
    TimeProvider timeProvider,
    IOptions<GameNotificationOptions> options
) : TimedBackgroundService(options.Value.BackgroundService, timeProvider)
{
    private readonly RedisValue ConsumerName = $"tenuki-broadcaster-{Guid.NewGuid():N}";

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await reader.InitializeAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteTickAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        await reader.ReadIncomingMessagesAsync(ConsumerName, stoppingToken);
    }
}
