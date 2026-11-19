using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Services.Logic;

internal static class CountingStateExtensions
{
    extension(CountingState state)
    {
        public CountingState MarkDeadStones(ImmutableHashSet<Point> points) =>
            state with
            {
                DeadStones = state.DeadStones.Union(points),
            };

        public CountingState UnmarkDeadStones(ImmutableHashSet<Point> points) =>
            state with
            {
                DeadStones = state.DeadStones.Except(points),
            };

        public CountingState AcceptScore(PlayerColor color) =>
            state with
            {
                AcceptedScore = state.AcceptedScore.Add(color),
            };

        public bool ScoreFinished => state.AcceptedScore.Count == 2;
    }
}
