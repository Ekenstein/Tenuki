using System.Collections.Immutable;

namespace Tenuki.Sgf;

public sealed record SgfProperty(string Identifier, ImmutableList<string> Values);
