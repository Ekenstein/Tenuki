using System.Collections.Immutable;

namespace Tenuki.Sgf.Tests;

public enum ValueType
{
    None,
    Number,
    Real,
    Double,
    Color,
    SimpleText,
    Text,
    Point,
}

public sealed record GeneratedPropertyValue(ValueType Type, string Value);

public sealed record GeneratedProperty(
    string Identifier,
    ImmutableList<GeneratedPropertyValue> Values
);

public sealed record GeneratedNode(ImmutableList<GeneratedProperty> Properties);

public sealed record GeneratedTree(
    ImmutableList<GeneratedNode> Sequence,
    ImmutableList<GeneratedTree> Trees
);

public static class GeneratedTypes
{
    public static SgfGameTree ToSgfGameTree(this GeneratedTree tree)
    {
        var sequence = tree.Sequence.Select(ToSgfNode);
        var trees = tree.Trees.Select(ToSgfGameTree);

        return new([.. sequence], [.. trees]);
    }

    public static SgfNode ToSgfNode(this GeneratedNode node)
    {
        var properties = node.Properties.Select(ToSgfProperty);
        return new([.. properties]);
    }

    public static SgfProperty ToSgfProperty(this GeneratedProperty property)
    {
        var values = property.Values.Select(x => x.Value);
        return new(property.Identifier, [.. values]);
    }
}
