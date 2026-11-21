using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Tenuki.Commons;
using Tenuki.Services.Logic.Game;

namespace Tenuki.Services.Tests.Logic.Game;

public class ClockStateTests
{
    private static Gen<(int PeriodSeconds, long Remaining, long Elapsed)> PeriodTransitionGen =>
        from seconds in Gen.Choose(1, 60)
        let periodLength = seconds * 1000L
        from remaining in Gen.Choose(1, (int)periodLength - 1)
        from overtime in Gen.Choose(1, (int)periodLength - 1)
        select (
            PeriodSeconds: seconds,
            Remaining: (long)remaining,
            Elapsed: (long)remaining + overtime
        );

    [Property]
    public bool Fixed_Time_Subtracts_Elapsed_Time(NonNegativeInt remaining, NonNegativeInt elapsed)
    {
        var playerClock = new PlayerClock(remaining.Get, 0, 0);
        var settings = new TimeSettings.Fixed(300);
        var updated = playerClock.UpdateClock(settings, elapsed.Get);
        var expected = Math.Max(0, remaining.Get - elapsed.Get);
        return updated.MainTimeRemainingMs == expected;
    }

    [Property]
    public bool Fixed_Time_Never_Increases(NonNegativeInt remaining, NonNegativeInt elapsed)
    {
        var playerClock = new PlayerClock(remaining.Get, 0, 0);
        var settings = new TimeSettings.Fixed(300);
        var updated = playerClock.UpdateClock(settings, elapsed.Get);
        return updated.MainTimeRemainingMs <= playerClock.MainTimeRemainingMs;
    }

    [Property]
    public bool Fixed_Time_Main_Time_Is_Monotonic(
        NonNegativeInt remaining,
        NonNegativeInt elapsedOne,
        NonNegativeInt elapsedTwo
    )
    {
        var playerClock = new PlayerClock(remaining.Get, 0, 0);
        var settings = new TimeSettings.Fixed(300);
        var t1 = Math.Min(elapsedOne.Get, elapsedTwo.Get);
        var t2 = Math.Max(elapsedOne.Get, elapsedTwo.Get);
        var updatedAtT1 = playerClock.UpdateClock(settings, t1);
        var updatedAtT2 = playerClock.UpdateClock(settings, t2);

        return updatedAtT2.MainTimeRemainingMs <= updatedAtT1.MainTimeRemainingMs;
    }

    [Property]
    public bool ByoYomi_Time_Main_Time_Is_Monotonic(
        NonNegativeInt remaining,
        NonNegativeInt elapsedOne,
        NonNegativeInt elapsedTwo
    )
    {
        var playerClock = new PlayerClock(remaining.Get, 0, 0);
        var settings = new TimeSettings.ByoYomi(300, 3, 300);
        var t1 = Math.Min(elapsedOne.Get, elapsedTwo.Get);
        var t2 = Math.Max(elapsedOne.Get, elapsedTwo.Get);
        var updatedAtT1 = playerClock.UpdateClock(settings, t1);
        var updatedAtT2 = playerClock.UpdateClock(settings, t2);
        return updatedAtT2.MainTimeRemainingMs <= updatedAtT1.MainTimeRemainingMs;
    }

    [Property]
    public bool ByoYomi_Time_Periods_Is_Monotonic(
        NonNegativeInt remaining,
        NonNegativeInt elapsedOne,
        NonNegativeInt elapsedTwo
    )
    {
        var playerClock = new PlayerClock(remaining.Get, 3, 300);
        var settings = new TimeSettings.ByoYomi(300, 3, 300);
        var t1 = Math.Min(elapsedOne.Get, elapsedTwo.Get);
        var t2 = Math.Max(elapsedOne.Get, elapsedTwo.Get);
        var updatedAtT1 = playerClock.UpdateClock(settings, t1);
        var updatedAtT2 = playerClock.UpdateClock(settings, t2);
        return updatedAtT2.RemainingPeriods <= updatedAtT1.RemainingPeriods;
    }

    [Property]
    public bool ByoYomi_Time_Periods_Is_Never_Increased(
        NonNegativeInt remaining,
        NonNegativeInt elapsed
    )
    {
        var playerClock = new PlayerClock(remaining.Get, 3, 300);
        var settings = new TimeSettings.ByoYomi(300, 3, 300);
        var updated = playerClock.UpdateClock(settings, elapsed.Get);
        return updated.RemainingPeriods <= playerClock.RemainingPeriods;
    }

    [Property]
    public bool Main_Time_Is_Consumed_Before_ByoYomi(
        NonNegativeInt mainTime,
        NonNegativeInt elapsed
    )
    {
        var clock = new PlayerClock(
            MainTimeRemainingMs: mainTime.Get,
            RemainingPeriods: 5,
            PeriodTimeRemainingMs: 30_000
        );

        var settings = new TimeSettings.ByoYomi(300, 5, 30);

        var elapsedMs = Math.Min(elapsed.Get, clock.MainTimeRemainingMs - 1);

        var updated = clock.UpdateClock(settings, elapsedMs);

        return updated.MainTimeRemainingMs == clock.MainTimeRemainingMs - elapsedMs
            && updated.RemainingPeriods == clock.RemainingPeriods
            && updated.PeriodTimeRemainingMs == clock.PeriodTimeRemainingMs;
    }

    [Property]
    public Property CrossingPeriodBoundaryResetsPeriodTime()
    {
        return Prop.ForAll(PeriodTransitionGen.ToArbitrary(), values =>
        {
            var settings = new TimeSettings.ByoYomi(300, 3, values.PeriodSeconds * 1000L);

            var clock = new PlayerClock(
                MainTimeRemainingMs: 0,
                RemainingPeriods: 3,
                PeriodTimeRemainingMs: values.Remaining
            );

            var updated = clock.UpdateClock(settings, values.Elapsed);

            var expectedRemaining =
                values.PeriodSeconds * 1000L - (values.Elapsed - values.Remaining);

            return updated.RemainingPeriods == 2
                && updated.PeriodTimeRemainingMs == expectedRemaining;
        });
    }
}
