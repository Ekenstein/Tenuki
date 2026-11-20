using System.Threading.Channels;
using Microsoft.Extensions.Time.Testing;
using Tenuki.Server.BackgroundServices;
using Tenuki.Server.Options;
using Xunit;

namespace Tenuki.Server.Tests.BackgroundServices;

public sealed class TimedBackgroundServiceTests
{
    [Fact]
    public async Task Executes_Tick_After_Configured_Delay()
    {
        var options = new TimedBackgroundServiceOptions { DelayInMs = 500 };

        var timeProvider = new FakeTimeProvider();

        using var service = new TestTimedBackgroundService(options, timeProvider);

        await service.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await service.WaitForTimerToStartAsync(TestContext.Current.CancellationToken);

            // verify that that the background service doesn't run too early.
            timeProvider.Advance(service.Period - TimeSpan.FromTicks(1));
            Assert.False(service.Ticks.Reader.TryRead(out _));

            timeProvider.Advance(TimeSpan.FromTicks(1));

            await service.WaitForTickAsync(1, TestContext.Current.CancellationToken);

            Assert.True(service.Ticks.Reader.TryRead(out _));
            Assert.False(service.Ticks.Reader.TryRead(out _));

            timeProvider.Advance(TimeSpan.FromMilliseconds(options.DelayInMs));
            await service.WaitForTickAsync(2, TestContext.Current.CancellationToken);

            Assert.True(service.Ticks.Reader.TryRead(out _));
            Assert.False(service.Ticks.Reader.TryRead(out _));
        }
        finally
        {
            await service.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private sealed class TestTimedBackgroundService(
        TimedBackgroundServiceOptions options,
        TimeProvider timeProvider
    ) : TimedBackgroundService(options, timeProvider)
    {
        public Channel<bool> Ticks { get; } = Channel.CreateUnbounded<bool>();

        protected override Task ExecuteTickAsync(CancellationToken stoppingToken)
        {
            Ticks.Writer.TryWrite(true);
            return Task.CompletedTask;
        }
    }
}
