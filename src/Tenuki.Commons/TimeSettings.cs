using System.Text.Json.Serialization;

namespace Tenuki.Commons;

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record class TimeSettings(long MainTimeMs)
{
    public sealed record class Fixed(long MainTimeMs) : TimeSettings(MainTimeMs);

    public sealed record class ByoYomi(long MainTimeMs, int Periods, long PeriodTimeMs)
        : TimeSettings(MainTimeMs);
}
