using Tenuki.Server.Options;

namespace Tenuki.Server.BackgroundServices;

internal abstract class TimedBackgroundService(
    TimedBackgroundServiceOptions options,
    TimeProvider timeProvider
) : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromMilliseconds(options.DelayInMs);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExecuteTickAsync(stoppingToken);
        }
    }

    protected abstract Task ExecuteTickAsync(CancellationToken stoppingToken);
}
