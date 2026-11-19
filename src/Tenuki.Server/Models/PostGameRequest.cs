using Tenuki.Commons;

namespace Tenuki.Server.Models;

public sealed record PostGameRequest(
    int BoardSize,
    double Komi,
    int Handicap,
    TimeSettings TimeSettings
)
{
    public static readonly PostGameRequest Default = new(
        19,
        6.5,
        0,
        new TimeSettings.ByoYomi(MainTimeMs: 5 * 60 * 1000, Periods: 5, PeriodTimeMs: 30 * 1000)
    );
};
