using System.Diagnostics.CodeAnalysis;

namespace Tenuki.Utils;

public abstract record Id<TId>(Guid Value) : IParsable<TId>, IComparable<TId>
    where TId : Id<TId>
{
    public static TId Parse(string s, IFormatProvider? provider) =>
        CreateInstance(Guid.Parse(s, provider));

    public static bool TryParse(
        string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out TId result
    )
    {
        if (Guid.TryParse(s, provider, out var guid))
        {
            result = CreateInstance(guid);
            return true;
        }

        result = default;
        return false;
    }

    private static TId CreateInstance(Guid value) =>
        Activator.CreateInstance(typeof(TId), value) as TId
        ?? throw new InvalidOperationException($"Failed to create instance of {typeof(TId).Name}");

    public int CompareTo(TId? other) => Value.CompareTo(other?.Value);
}
