using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Tenuki.Utils;

public sealed class SecretTokenHash : IParsable<SecretTokenHash>
{
    private const int Size = SHA256.HashSizeInBytes;
    private readonly byte[] _bytes;

    private SecretTokenHash(ReadOnlySpan<byte> bytes)
    {
        _bytes = bytes.ToArray();
    }

    public static SecretTokenHash Hash(SecretToken token) => new(SHA256.HashData(token.Bytes));

    public string ToHexString() => Convert.ToHexString(_bytes);

    public static SecretTokenHash Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);

        return TryParse(s, provider, out var hash)
            ? hash
            : throw new FormatException("Invalid secret token hash.");
    }

    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out SecretTokenHash result
    )
    {
        result = null;

        if (s is null || s.Length != Size * 2)
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[Size];

        var status = Convert.FromHexString(s, buffer, out var charsConsumed, out var bytesWritten);

        if (status != OperationStatus.Done || charsConsumed != s.Length || bytesWritten != Size)
        {
            return false;
        }

        result = new SecretTokenHash(buffer);
        return true;
    }

    public ReadOnlySpan<byte> Bytes => _bytes;
};
