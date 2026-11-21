using Tenuki.Commons;

namespace Tenuki.Services.Logic.Game;

internal static class PlayerClockExtensions
{
    private static readonly PlayerClock ConsumedClock = new(0, 0, 0);

    internal static PlayerClock UpdateClock(
        this PlayerClock clock,
        TimeSettings settings,
        long elapsedMs
    ) =>
        settings switch
        {
            TimeSettings.Fixed _ => clock.UpdateClock(elapsedMs),
            TimeSettings.ByoYomi byoYomiSettings => clock.UpdateClock(byoYomiSettings, elapsedMs),
        };

    private static PlayerClock UpdateClock(this PlayerClock clock, long elapsedMs) =>
        clock with
        {
            MainTimeRemainingMs = Math.Max(0, clock.MainTimeRemainingMs - elapsedMs),
        };

    internal static PlayerClock UpdateClock(
        this PlayerClock clock,
        TimeSettings.ByoYomi settings,
        long elapsedMs
    )
    {
        // if main time consumed is the time that has elapsed, we know that we still have main time left.
        var mainTimeConsumed = Math.Min(clock.MainTimeRemainingMs, elapsedMs);

        var mainTimeRemaining = clock.MainTimeRemainingMs - mainTimeConsumed;

        // The amount of overtime we have consumed.
        var overtimeElapsed = elapsedMs - mainTimeConsumed;

        // we still have main time left, so just tick it down the elapsed time.
        if (overtimeElapsed == 0)
        {
            return clock with { MainTimeRemainingMs = mainTimeRemaining };
        }

        var periodsRemaining = clock.RemainingPeriods;
        var periodTimeRemaining = clock.PeriodTimeRemainingMs;

        if (overtimeElapsed >= periodTimeRemaining)
        {
            overtimeElapsed -= periodTimeRemaining;
            periodsRemaining--;

            if (periodsRemaining <= 0)
            {
                return ConsumedClock;
            }

            var consumedPeriods = (int)(overtimeElapsed / settings.PeriodTimeMs);
            periodsRemaining -= consumedPeriods;

            if (periodsRemaining <= 0)
            {
                return ConsumedClock;
            }

            overtimeElapsed %= settings.PeriodTimeMs;

            periodTimeRemaining = settings.PeriodTimeMs;
        }

        return clock with
        {
            MainTimeRemainingMs = mainTimeRemaining,
            PeriodTimeRemainingMs = periodTimeRemaining - overtimeElapsed,
            RemainingPeriods = periodsRemaining,
        };
    }

    internal static PlayerClock CompleteTurn(this PlayerClock clock, TimeSettings settings) =>
        settings switch
        {
            TimeSettings.Fixed => clock,
            TimeSettings.ByoYomi byoYomiSettings => clock.CompleteTurn(byoYomiSettings),
        };

    internal static PlayerClock CompleteTurn(this PlayerClock clock, TimeSettings.ByoYomi settings)
    {
        if (clock.MainTimeRemainingMs > 0 || clock.PeriodTimeRemainingMs <= 0)
        {
            return clock;
        }

        return clock with
        {
            // reset the period
            PeriodTimeRemainingMs = settings.PeriodTimeMs,
        };
    }

    extension(PlayerClock clock)
    {
        internal bool IsTimeOver =>
            clock.RemainingPeriods <= 0
            && clock.MainTimeRemainingMs <= 0
            && clock.PeriodTimeRemainingMs <= 0;
    }
}
