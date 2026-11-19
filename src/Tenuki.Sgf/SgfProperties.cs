using System.Globalization;

namespace Tenuki.Sgf;

public static class SgfProperties
{
    public static SgfProperty FileFormat(int version) => new("FF", [Number(version)]);

    public static SgfProperty BoardSize(int size) => new("SZ", [Number(size)]);

    public static SgfProperty Komi(double komi) => new("KM", [Real(komi)]);

    public static SgfProperty Handicap(int handicap) => new("HA", [Number(handicap)]);

    public static SgfProperty GameType(int gameType) => new("GM", [Number(gameType)]);

    public static SgfProperty Date(DateTime date) => new("DT", [date.ToString("yyyy-MM-dd")]);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Real(double value) => value.ToString(CultureInfo.InvariantCulture);
}
