using Tenuki.Server.Options;

namespace Tenuki.Server.BackgroundServices;

public abstract class TimedBackgroundService(
    TimedBackgroundServiceOptions options,
    TimeProvider timeProvider
) : BackgroundService
{
    internal readonly TimeSpan Period = TimeSpan.FromMilliseconds(options.DelayInMs);

    private readonly TaskCompletionSource _started = NewCompletionSource();
    private readonly Lock _sync = new();
    private TaskCompletionSource _nextTick = NewCompletionSource();
    private long _completedTicks;

    internal Task WaitForTimerToStartAsync(CancellationToken cancellationToken) =>
        _started.Task.WaitAsync(cancellationToken);

    internal async Task WaitForTickAsync(long tick, CancellationToken cancellationToken)
    {
        while (true)
        {
            Task nextTick;
            lock (_sync)
            {
                if (_completedTicks >= tick)
                {
                    return;
                }

                nextTick = _nextTick.Task;
            }

            await nextTick.WaitAsync(cancellationToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period, timeProvider);

        _started.TrySetResult();

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExecuteTickAsync(stoppingToken);

                TaskCompletionSource completed;
                lock (_sync)
                {
                    _completedTicks++;
                    completed = _nextTick;
                    _nextTick = NewCompletionSource();
                }

                completed.TrySetResult();
            }
        }
        finally
        {
            lock (_sync)
            {
                _nextTick.TrySetCanceled(stoppingToken);
            }
        }
    }

    protected abstract Task ExecuteTickAsync(CancellationToken stoppingToken);

    private static TaskCompletionSource NewCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
