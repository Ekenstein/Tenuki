using FsCheck.Xunit;

namespace Tenuki.Utils.Tests;

public sealed class GroupByAdjacentTests
{
    [Property]
    public bool GroupAdjacentBy_With_AlwaysFalse_Predicate_Creates_One_Group_Per_Element(
        int[] source
    )
    {
        var result = source.GroupAdjacentBy((a, b) => false).ToArray();
        return result.Length == source.Length;
    }

    [Property]
    public bool GroupCount_Never_Exceeds_SourceLength(int[] source)
    {
        var groups = source.GroupAdjacentBy((a, b) => a == b);
        return groups.Count() <= source.Length;
    }

    [Property]
    public bool Grouping_Preserves_Elements_And_Order(int[] source)
    {
        var result = source.GroupAdjacentBy((a, b) => a == b).SelectMany(x => x);
        return source.SequenceEqual(result);
    }

    [Property]
    public bool Groups_Are_Never_Empty(int[] source)
    {
        return source.GroupAdjacentBy((a, b) => a == b).All(group => group.Count > 0);
    }

    [Property]
    public bool All_Elements_In_Group_Satisfies_Predicate(int[] source)
    {
        return source
            .GroupAdjacentBy(Predicate)
            .All(group => group.Zip(group.Skip(1)).All(pair => Predicate(pair.First, pair.Second)));
        static bool Predicate(int a, int b) => a == b;
    }

    [Property]
    public bool Adjacent_Groups_Cannot_Be_Merged(int[] source)
    {
        var groups = source.GroupAdjacentBy((a, b) => a == b).ToArray();
        return groups.Zip(groups.Skip(1)).All(pair => pair.First[^1] != pair.Second[0]);
    }

    [Property]
    public bool GroupCount_Equals_Number_Of_Transitions_Plus_One(int[] source)
    {
        var expected =
            source.Length == 0
                ? 0
                : 1 + source.Zip(source.Skip(1)).Count(pair => pair.First != pair.Second);

        var actual = source.GroupAdjacentBy((a, b) => a == b).Count();

        return expected == actual;
    }

    [Property]
    public bool AlwaysTrue_Predicate_Produces_At_Most_One_Group(int[] source)
    {
        var groups = source.GroupAdjacentBy((a, b) => true);

        return groups.Count() == Math.Min(source.Length, 1);
    }
}
