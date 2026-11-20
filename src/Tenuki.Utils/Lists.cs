using System.Collections.Immutable;
using Medallion.Collections;

namespace Tenuki.Utils;

public static class Lists
{
    public static ImmutableList<T> ListOf<T>(params T[] items) => [.. items];

    public static ImmutableList<T> NonEmptyListOf<T>(T head, params T[] tail) => [head, .. tail];

    public static ImmutableList<T>.Builder MutableListOf<T>(params T[] items) =>
        ListOf(items).ToBuilder();

    public static ImmutableDictionary<TKey, TValue> MapOf<TKey, TValue>(
        params KeyValuePair<TKey, TValue>[] pairs
    )
        where TKey : notnull => [.. pairs];

    public static KeyValuePair<TKey, TValue> PairOf<TKey, TValue>(TKey key, TValue value)
        where TKey : notnull => new(key, value);

    public static ImmutableLinkedList<T> LinkedListOf<T>(params T[] items) => [.. items];

    public static Zipper<T> ToZipper<T>(this ImmutableList<T> ts)
    {
        if (ts.IsEmpty)
        {
            throw new ArgumentException("Can't convert an empty enumerable to a zipper");
        }
        var head = ts[0];
        var tail = ts.RemoveAt(0);

        return new Zipper<T>([], head, [.. tail]);
    }

    public static ImmutableLinkedList<T> Reverse<T>(
        this ImmutableLinkedList<T> list,
        ImmutableLinkedList<T> result
    )
    {
        static ImmutableLinkedList<T> Inner(
            ImmutableLinkedList<T> list,
            ImmutableLinkedList<T> result
        )
        {
            if (list.Count == 0)
            {
                return result;
            }

            return Inner(list.Tail, [list.Head, .. result]);
        }

        return Inner(list, result);
    }

    public static ImmutableList<T>? InitOrNull<T>(this ImmutableList<T> list) =>
        list.IsEmpty ? null : list.RemoveAt(list.Count - 1);

    public static ImmutableList<T> RemoveLastUntil<T>(
        this ImmutableList<T> list,
        Predicate<T> predicate
    )
    {
        var index = list.FindLastIndex(predicate);
        if (index < 0)
        {
            return [];
        }

        return list.RemoveRange(index + 1, list.Count - 1);
    }

    public static IEnumerable<U> SelectNotNull<T, U>(
        this IEnumerable<T> ts,
        Func<T, U?> selector
    ) => ts.Select(selector).Where(x => x != null).Select(x => x!);

    public static IEnumerable<ImmutableList<T>> GroupAdjacentBy<T>(
        this IEnumerable<T> source,
        Func<T, T, bool> predicate
    )
    {
        using var enumerator = source.GetEnumerator();

        if (!enumerator.MoveNext())
        {
            yield break;
        }

        var previous = enumerator.Current;
        var batch = ListOf(previous).ToBuilder();

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;

            if (!predicate(previous, current))
            {
                yield return batch.ToImmutable();
                batch.Clear();
            }

            previous = current;
            batch.Add(current);
        }

        yield return batch.ToImmutable();
    }
}
