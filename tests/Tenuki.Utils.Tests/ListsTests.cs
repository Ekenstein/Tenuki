using FsCheck.Xunit;

namespace Tenuki.Utils.Tests;

public sealed class ListsTests
{
    [Property]
    public bool ListOf_Has_Count_EqualTo_Length_Of_Items(int[] items)
    {
        var result = ListOf(items);
        var expected = items.Length;
        return result.Count == expected;
    }

    [Property]
    public bool NonEmptyListOf_Has_Count_EqualTo_1_Plus_Count_Of_Tail(int head, int[] tail)
    {
        var result = NonEmptyListOf(head, tail);
        var expected = 1 + tail.Length;
        return result.Count == expected;
    }

    [Property]
    public bool NonEmptyListOf_Has_Head_As_First_Item(int head, int[] tail)
    {
        var result = NonEmptyListOf(head, tail);
        return result[0] == head;
    }
}
