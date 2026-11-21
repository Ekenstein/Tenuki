using Tenuki.Commons;

namespace Tenuki.Services.Logic.Game;

internal static class TimeSettingsExtensions
{
    extension(TimeSettings timeSettings)
    {
        internal IEnumerable<Error> Validate() =>
            timeSettings switch
            {
                TimeSettings.Fixed fixedTime => ValidateFixedTimeSettings(fixedTime),
                TimeSettings.ByoYomi byoYomi => ValidateByoYomiTimeSettings(byoYomi),
            };

        private static IEnumerable<Error> ValidateFixedTimeSettings(TimeSettings.Fixed fixedTime)
        {
            if (fixedTime.MainTimeMs <= 0)
            {
                yield return GameLogicErrors.InvalidTimeSettings;
            }
        }

        private static IEnumerable<Error> ValidateByoYomiTimeSettings(TimeSettings.ByoYomi byoYomi)
        {
            var errors = new List<Error>();
            if (byoYomi.MainTimeMs <= 0)
            {
                yield return GameLogicErrors.InvalidTimeSettings;
            }

            if (byoYomi.PeriodTimeMs <= 0)
            {
                yield return GameLogicErrors.InvalidTimeSettings;
            }

            if (byoYomi.Periods <= 0)
            {
                yield return GameLogicErrors.InvalidTimeSettings;
                errors.Add(GameLogicErrors.InvalidTimeSettings);
            }
        }

        internal ClockState GetInitialClockState(DateTimeOffset now)
        {
            var playerClock = timeSettings.GetInitialPlayerClock();
            return new(playerClock, playerClock, now);
        }

        private PlayerClock GetInitialPlayerClock() =>
            timeSettings switch
            {
                TimeSettings.ByoYomi byoYomiSettings => new(
                    byoYomiSettings.MainTimeMs,
                    byoYomiSettings.Periods,
                    byoYomiSettings.PeriodTimeMs
                ),
                TimeSettings.Fixed fixedSettings => new(fixedSettings.MainTimeMs, 0, 0),
            };
    }
}
