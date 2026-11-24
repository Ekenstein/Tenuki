using FsCheck;
using FsCheck.Fluent;
using Tenuki.Tests.Utils;

namespace Tenuki.Sgf.Tests;

public static class SgfArbitraries
{
    private static readonly Arbitrary<string> ColorValue = Gen.Elements("B", "W")
        .Let(gen =>
        {
            return Arb.From(gen, _ => []);
        });

    private static readonly Arbitrary<string> DoubleValue = Gen.Elements("1", "2")
        .Let(gen =>
        {
            return Arb.From(gen, _ => []);
        });

    private static readonly Arbitrary<string> PointValue = Gen.Choose('a', 'z')
        .Two()
        .Select(x => $"{(char)x.Item1}{(char)x.Item2}")
        .Let(gen => Arb.From(gen, _ => []));

    private static Arbitrary<string> PropertyValueByType(ValueType valueType) =>
        valueType switch
        {
            ValueType.None => Arbitraries.EmptyString,
            ValueType.Number => Arbitraries.NumberString,
            ValueType.Real => Arbitraries.RealNumberString,
            ValueType.Double => DoubleValue,
            ValueType.Color => ColorValue,
            ValueType.SimpleText => ArbMap.Default.ArbFor<string>(),
            ValueType.Text => ArbMap.Default.ArbFor<string>(),
            ValueType.Point => PointValue,
            _ => throw new NotImplementedException(),
        };

    private static readonly Arbitrary<GeneratedPropertyValue> ArbForPropertyValue = Gen.Elements(
            Enum.GetValues<ValueType>()
        )
        .SelectMany(
            valueType => PropertyValueByType(valueType).Generator,
            (valueType, value) => new GeneratedPropertyValue(valueType, value)
        )
        .Let(propertyValue =>
        {
            return Arb.From(propertyValue, ShrinkPropertyValue);
        });

    private static IEnumerable<GeneratedPropertyValue> ShrinkPropertyValue(
        GeneratedPropertyValue propertyValue
    )
    {
        var values = PropertyValueByType(propertyValue.Type).Shrinker(propertyValue.Value);
        return values.Select(value => new GeneratedPropertyValue(propertyValue.Type, value));
    }

    private static readonly Arbitrary<string> ArbForPropertyIdentifier = Gen.NonEmptyListOf(
            Generators.UppercaseChar
        )
        .Select(charList => new string([.. charList]))
        .Let(gen => Arb.From(gen, ShrinkPropertyIdentifier));

    private static IEnumerable<string> ShrinkPropertyIdentifier(string uppercaseString)
    {
        if (uppercaseString.Length <= 1)
        {
            return [];
        }

        return Enumerable.Range(1, uppercaseString.Length - 1).Select(i => uppercaseString[..i]);
    }

    public static readonly Arbitrary<GeneratedProperty> ArbForProperty = ArbForPropertyIdentifier
        .Generator.SelectMany(
            identifier => Gen.NonEmptyListOf(ArbForPropertyValue.Generator),
            (identifier, values) => new GeneratedProperty(identifier, [.. values])
        )
        .Let(gen =>
        {
            return Arb.From(gen, ShrinkProperty);
        });

    private static IEnumerable<GeneratedProperty> ShrinkProperty(GeneratedProperty property)
    {
        var (identifier, values) = property;
        var v1 = ArbForPropertyIdentifier
            .Shrinker(identifier)
            .Select(x => new GeneratedProperty(x, values));
        var v2 = Arb.Array(ArbForPropertyValue)
            .Shrinker([.. values])
            .Where(x => x.Length > 0)
            .Select(x => new GeneratedProperty(identifier, [.. x]));

        return v1.Concat(v2).Distinct();
    }

    public static readonly Arbitrary<GeneratedNode> ArbForNode = Arb.Array(ArbForProperty)
        .Convert(properties => new GeneratedNode([.. properties]), node => [.. node.Properties]);

    private static readonly Arbitrary<GeneratedNode[]> ArbForSequence = Arb.Array(ArbForNode)
        .Filter(nodes => nodes.Length > 0);

    private static Gen<GeneratedTree> TreeGenerator(int size) =>
        from sequence in ArbForSequence.Generator
        from trees in size <= 0
            ? Gen.Constant(Array.Empty<GeneratedTree>())
            : Gen.Choose(0, Math.Min(size, 3))
                .SelectMany(count => Gen.ArrayOf(TreeGenerator(size / 2), count))
        select new GeneratedTree([.. sequence], [.. trees]);

    private static IEnumerable<GeneratedTree> ShrinkTree(GeneratedTree tree)
    {
        var sequence = tree.Sequence.ToArray();
        var trees = tree.Trees.ToArray();

        // Shrink sequence
        foreach (var shrunk in ArbForSequence.Shrinker(sequence))
        {
            yield return tree with
            {
                Sequence = [.. shrunk],
            };
        }

        // Remove child trees
        for (var i = 0; i < trees.Length; i++)
        {
            yield return tree with
            {
                Trees = [.. trees.Where((_, index) => index != i)],
            };
        }

        // Shrink individual child trees recursively
        for (var i = 0; i < trees.Length; i++)
        {
            foreach (var shrunk in ShrinkTree(trees[i]))
            {
                var children = (GeneratedTree[])trees.Clone();
                children[i] = shrunk;

                yield return tree with
                {
                    Trees = [.. children],
                };
            }
        }
    }

    public static Arbitrary<GeneratedTree> ArbForTree()
    {
        var generator = Gen.Sized(TreeGenerator);
        return Arb.From(generator, ShrinkTree);
    }
}
