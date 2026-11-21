using Tenuki.Commons;

namespace Tenuki.Services.Logic.Game;

internal static class GameSettingsExtensions
{
    extension(GameSettings settings)
    {
        internal BoardPosition InitialBoardPosition =>
            BoardPosition.Empty.AddStones(
                settings.HandicapPoints.Select(x => x.ToStone(PlayerColor.Black))
            );

        internal IEnumerable<Point> BoardIntersections =>
            Enumerable
                .Range(0, settings.BoardSize * settings.BoardSize)
                .Select(n =>
                {
                    var x = (n % settings.BoardSize);
                    var y = (n / settings.BoardSize);
                    return new Point(x, y);
                });

        internal IEnumerable<Error> Validate()
        {
            if (settings.BoardSize <= 0)
            {
                yield return GameLogicErrors.InvalidBoardSize;
            }
            else if (!settings.ValidHandicaps.Contains(settings.Handicap))
            {
                yield return GameLogicErrors.InvalidHandicap;
            }

            foreach (var error in settings.TimeSettings.Validate())
            {
                yield return error;
            }
        }

        internal Point? Tengen =>
            settings.BoardSize > 0 && settings.BoardSize % 2 != 0
                ? new Point(settings.BoardSize / 2, settings.BoardSize / 2)
                : null;

        internal IEnumerable<int> ValidHandicaps =>
            settings.BoardSize switch
            {
                < 7 => [0],
                7 => [0, .. Enumerable.Range(2, 3)],
                _ when settings.BoardSize % 2 == 0 => [0, .. Enumerable.Range(2, 3)],
                _ => [0, .. Enumerable.Range(2, 8)],
            };

        internal int? EdgeDistance =>
            settings.BoardSize switch
            {
                < 7 => null,
                < 13 => 3,
                _ => 4,
            };

        internal IEnumerable<Point> HandicapPoints =>
            settings.EdgeDistance?.Let(edgeDistance =>
            {
                var near = edgeDistance - 1;
                var far = settings.BoardSize - edgeDistance;

                Point[] corners = [new(near, far), new(far, near), new(far, far), new(near, near)];

                if (settings.Tengen is not { } tengen)
                {
                    return settings.Handicap switch
                    {
                        2 => corners[..2],
                        3 => corners[..3],
                        4 => corners,
                        _ => [],
                    };
                }

                Point[] sides =
                [
                    new(near, tengen.Y),
                    new(far, tengen.Y),
                    new(tengen.X, near),
                    new(tengen.X, far),
                ];

                return settings.Handicap switch
                {
                    2 => corners[..2],
                    3 => corners[..3],
                    4 => corners,
                    5 => [.. corners, tengen],
                    6 => [.. corners, .. sides[..2]],
                    7 => [.. corners, .. sides[..2], tengen],
                    8 => [.. corners, .. sides],
                    9 => [.. corners, .. sides, tengen],
                    _ => [],
                };
            }) ?? [];
    }
}
