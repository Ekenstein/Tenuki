using Tenuki.Commons;

namespace Tenuki.Services.Logic;

internal static class PointExtensions
{
    internal static IEnumerable<Point> GetAdjacentPoints(this Point stone, GameSettings settings)
    {
        var allAdjacentPoints = ListOf<Point>(
            stone with
            {
                X = stone.X + 1,
            },
            stone with
            {
                X = stone.X - 1,
            },
            stone with
            {
                Y = stone.Y + 1,
            },
            stone with
            {
                Y = stone.Y - 1,
            }
        );

        var width = settings.BoardSize;
        var height = settings.BoardSize;

        return allAdjacentPoints.Where(point => !point.IsOutOfBounds(settings));
    }

    internal static bool IsOutOfBounds(this Point point, GameSettings settings)
    {
        return point.X < 0
            || point.X >= settings.BoardSize
            || point.Y < 0
            || point.Y >= settings.BoardSize;
    }

    internal static Stone ToStone(this Point point, PlayerColor color) => new(color, point);
}
