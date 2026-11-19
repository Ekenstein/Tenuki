namespace Tenuki.Commons;

public sealed record GameSettings(
    int BoardSize,
    double Komi,
    int Handicap,
    TimeSettings TimeSettings
)
{
    public static readonly GameSettings Default = new(19, 6.5, 0, new TimeSettings.Fixed(60 * 30));
};
