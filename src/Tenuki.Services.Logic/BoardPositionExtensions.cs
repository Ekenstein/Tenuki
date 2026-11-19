using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Services.Logic;

internal static class BoardPositionExtensions
{
    extension(BoardPosition board)
    {
        internal Score GetScore(GameSettings settings)
        {
            var territoriesByColor = board.GetTerritories(settings);
            var blackTerritory = territoriesByColor[PlayerColor.Black].Count;
            var whiteTerritory = territoriesByColor[PlayerColor.White].Count;

            var score = blackTerritory - (whiteTerritory + settings.Komi);

            return score switch
            {
                < 0 => new Score(PlayerColor.White, Math.Abs(score)),
                _ => new Score(PlayerColor.Black, score),
            };
        }

        internal Result<BoardPosition> PlaceStone(Stone stone, GameSettings settings)
        {
            if (stone.Point.IsOutOfBounds(settings))
            {
                return new Failure([GameLogicErrors.StoneOutOfBounds]);
            }

            if (board.Position.Any(x => x.Point == stone.Point))
            {
                return new Failure([GameLogicErrors.PointOccupied]);
            }

            var boardWithPlacedStone = board with
            {
                Position = board.Position.Add(stone),
                Hash = board.Hash ^ Zobrist.Hash(stone),
            };

            var enemyColor = stone.Color.Opponent;

            var enemyAdjacentStones = stone
                .Point.GetAdjacentPoints(settings)
                .Select(x => x.ToStone(enemyColor))
                .Where(board.Position.Contains);

            var updatedBoardAfterCaptures = enemyAdjacentStones.Aggregate(
                boardWithPlacedStone,
                (board, enemyStone) => CaptureGroupIfDead(board, enemyStone, settings)
            );

            if (!updatedBoardAfterCaptures.HasLiberties(stone, settings))
            {
                return new Failure([GameLogicErrors.Suicide]);
            }

            return new Success<BoardPosition>(updatedBoardAfterCaptures);
        }

        private BoardPosition CaptureGroupIfDead(Stone stone, GameSettings settings)
        {
            if (!board.Position.Contains(stone))
            {
                return board;
            }

            var group = board.GetGroup(stone, settings);
            if (group.HasLiberties)
            {
                return board;
            }

            var updatedBoard = board.RemoveStones(group.Stones);

            return stone.Color switch
            {
                PlayerColor.White => updatedBoard with
                {
                    BlackCaptures = updatedBoard.BlackCaptures + group.Stones.Count,
                },
                PlayerColor.Black => updatedBoard with
                {
                    WhiteCaptures = updatedBoard.WhiteCaptures + group.Stones.Count,
                },
                _ => throw new NotSupportedException(
                    $"The color '{stone.Color}' is not supported."
                ),
            };
        }

        public BoardPosition AddStones(IEnumerable<Stone> stones) =>
            stones.Aggregate(
                board,
                (board, stone) =>
                    board with
                    {
                        Position = board.Position.Add(stone),
                        Hash = board.Hash ^ Zobrist.Hash(stone),
                    }
            );

        public BoardPosition RemoveStones(IEnumerable<Point> points)
        {
            var stonesByPoint = board.Position.ToDictionary(x => x.Point);
            var stones = points.SelectNotNull(x => stonesByPoint.GetValueOrDefault(x));
            return board.RemoveStones(stones);
        }

        public BoardPosition RemoveStones(IEnumerable<Stone> stones) =>
            board with
            {
                Position = board.Position.Except(stones),
                Hash = stones.Aggregate(board.Hash, (hash, stone) => hash ^ Zobrist.Hash(stone)),
            };

        private bool HasLiberties(Stone stone, GameSettings settings) =>
            board.GetGroup(stone, settings).HasLiberties;

        private Group GetGroup(Stone stone, GameSettings settings)
        {
            var group = new HashSet<Stone>([stone]);
            var pending = new Stack<Stone>([stone]);
            var hasLiberties = false;

            while (pending.TryPop(out var current))
            {
                foreach (var point in current.Point.GetAdjacentPoints(settings))
                {
                    var adjacent = point.ToStone(stone.Color);

                    if (board.Position.Contains(adjacent))
                    {
                        if (group.Add(adjacent))
                        {
                            pending.Push(adjacent);
                        }
                    }
                    else if (!board.Position.Contains(point.ToStone(stone.Color.Opponent)))
                    {
                        hasLiberties = true;
                    }
                }
            }

            return new(group, hasLiberties);
        }

        private Dictionary<PlayerColor, HashSet<Point>> GetTerritories(GameSettings settings)
        {
            var unvisited = board.GetEmptyIntersections(settings).ToHashSet();
            var colorByPoint = board.Position.ToDictionary(x => x.Point, x => x.Color);
            var territories = new Dictionary<PlayerColor, HashSet<Point>>
            {
                [PlayerColor.Black] = [],
                [PlayerColor.White] = [],
            };

            while (unvisited.Count > 0)
            {
                var start = unvisited.First();
                var region = new HashSet<Point>();
                var pending = new Stack<Point>([start]);
                var borderingColors = new HashSet<PlayerColor>();

                while (pending.TryPop(out var point))
                {
                    region.Add(point);

                    foreach (var adjacent in point.GetAdjacentPoints(settings))
                    {
                        if (colorByPoint.TryGetValue(adjacent, out var color))
                        {
                            borderingColors.Add(color);
                        }
                        else if (unvisited.Remove(adjacent))
                        {
                            pending.Push(adjacent);
                        }
                    }
                }

                if (borderingColors.Count == 1)
                {
                    var borderingColor = borderingColors.Single();
                    territories[borderingColor].UnionWith(region);
                }
            }

            return territories;
        }

        private IEnumerable<Point> GetEmptyIntersections(GameSettings settings) =>
            settings.BoardIntersections.Where(point => !board.Position.Any(x => x.Point == point));
    }

    private sealed record Group(HashSet<Stone> Stones, bool HasLiberties);
}
