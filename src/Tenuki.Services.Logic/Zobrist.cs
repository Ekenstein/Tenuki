using Tenuki.Commons;

namespace Tenuki.Services.Logic;

internal static class Zobrist
{
    private const ulong Seed = 0x7965CBDDD4A9E7AF;

    internal static ulong Hash(Stone stone)
    {
        var value = Seed;

        value ^= (uint)stone.Point.X;
        value = Mix(value);

        value ^= (uint)stone.Point.Y;
        value = Mix(value);

        value ^= stone.Color switch
        {
            PlayerColor.Black => 0UL,
            PlayerColor.White => 1UL,
            _ => throw new NotSupportedException(),
        };

        return Mix(value);
    }

    private static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        value ^= value >> 31;

        return value;
    }
}
