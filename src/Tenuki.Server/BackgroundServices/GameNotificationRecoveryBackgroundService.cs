using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Tenuki.Server.Options;
using Tenuki.Server.Redis;

namespace Tenuki.Server.BackgroundServices;

internal sealed class GameNotificationRecoveryBackgroundService(
    RedisIncomingMessageReader reader,
    IOptions<GameNotificationRecoveryOptions> options,
    TimeProvider timeProvider
) : TimedBackgroundService(options.Value.BackgroundService, timeProvider)
{
    private readonly RedisValue ConsumerName = $"tenuki-recovery-{Guid.NewGuid():N}";
    private RedisValue _cursor = "0-0";

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await reader.InitializeAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteTickAsync(CancellationToken stoppingToken)
    {
        _cursor = await reader.RecoverPendingMessagesAsync(
            ConsumerName,
            minIdleTime: TimeSpan.FromMilliseconds(options.Value.MinIdleTimeInMs),
            startCursor: _cursor,
            maxNumberOfIterations: options.Value.MaxNumberOfIterations,
            batchSize: options.Value.BatchSize,
            stoppingToken
        );
    }
}
