using System.Collections.Immutable;

namespace Tenuki.Commons;

public sealed record CountingState(
    ImmutableHashSet<Point> DeadStones,
    ImmutableHashSet<PlayerColor> AcceptedScore
);
