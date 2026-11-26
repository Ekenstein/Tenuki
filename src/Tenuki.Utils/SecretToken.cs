using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Tenuki.Utils;

public sealed class SecretToken : IParsable<SecretToken>
{
    public const int Size = 32;
    private readonly byte[] _bytes;

    private SecretToken(ReadOnlySpan<byte> bytes)
    {
        _bytes = bytes.ToArray();
    }

    public ReadOnlySpan<byte> Bytes => _bytes;

    public string ToHexString() => Convert.ToHexString(_bytes);

    public bool Verify(SecretTokenHash hash) =>
        CryptographicOperations.FixedTimeEquals(ComputeHash().Bytes, hash.Bytes);

    public SecretTokenHash ComputeHash() => SecretTokenHash.Hash(this);

    public override string ToString() => "[REDACTED]";

    public static SecretToken Generate() => new(RandomNumberGenerator.GetBytes(Size));

    public static SecretToken Parse(string s, IFormatProvider? provider) =>
        TryParse(s, provider, out var token)
            ? token
            : throw new FormatException("Invalid ownership token.");

    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out SecretToken result
    )
    {
        if (s == null || s.Length != Size * 2)
        {
            result = null;
            return false;
        }

        Span<byte> buffer = stackalloc byte[Size];
        var status = Convert.FromHexString(s, buffer, out var charsConsumed, out var bytesWritten);

        if (status != OperationStatus.Done || charsConsumed != s.Length || bytesWritten != Size)
        {
            result = null;
            return false;
        }

        result = new(buffer);
        return true;
    }
}
