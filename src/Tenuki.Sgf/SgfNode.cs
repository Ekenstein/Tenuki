using System.Collections.Immutable;

namespace Tenuki.Sgf;

public sealed record SgfNode(ImmutableList<SgfProperty> Properties);
