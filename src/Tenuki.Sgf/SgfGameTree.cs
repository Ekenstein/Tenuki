using System.Collections.Immutable;

namespace Tenuki.Sgf;

public sealed record SgfGameTree(ImmutableList<SgfNode> Sequence, ImmutableList<SgfGameTree> Trees);
