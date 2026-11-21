using Tenuki.Commons;

namespace Tenuki.Services.Logic.Game;

internal static class ClockStateExtensions
{
    internal static long GetElapsedTime(this ClockState state, DateTimeOffset now)
    {
        var elapsed = now - state.TurnStartedAt;

        return Math.Max(0L, (long)elapsed.TotalMilliseconds);
    }

    internal static ClockState CompleteTurn(
        this ClockState state,
        PlayerColor player,
        TimeSettings timeSettings
    ) =>
        player switch
        {
            PlayerColor.Black => state with { Black = state.Black.CompleteTurn(timeSettings) },
            PlayerColor.White => state with { White = state.White.CompleteTurn(timeSettings) },
            _ => throw new NotSupportedException($"The color '{player}' is not supported."),
        };

    internal static ClockState UpdateClockState(
        this ClockState state,
        DateTimeOffset now,
        PlayerColor player,
        TimeSettings settings
    )
    {
        var elapsedMs = state.GetElapsedTime(now);
        return player switch
        {
            PlayerColor.Black => state with
            {
                Black = state.Black.UpdateClock(settings, elapsedMs),
            },
            PlayerColor.White => state with
            {
                White = state.White.UpdateClock(settings, elapsedMs),
            },
            _ => throw new NotSupportedException($"The color '{player}' is not supported."),
        };
    }
}
