using System.Collections.Immutable;

namespace Tenuki.Commons;

public sealed record BoardPosition(
    ImmutableHashSet<Stone> Position,
    int WhiteCaptures,
    int BlackCaptures,
    ulong Hash
)
{
    public static readonly BoardPosition Empty = new([], 0, 0, 0UL);
}
