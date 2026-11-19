using System.Security.Cryptography;
using System.Text;

namespace Tenuki.Utils;

public static class HmacTokenCodec
{
    public static readonly Error InvalidTokenFormat = new(
        nameof(InvalidTokenFormat),
        "Invalid token format."
    );
    public static readonly Error InvalidTokenSignature = new(
        nameof(InvalidTokenSignature),
        "Token signature failed validation"
    );
    public static readonly Error InvalidTokenEncoding = new(
        nameof(InvalidTokenEncoding),
        "Invalid token encoding."
    );

    public static string Encode(string payload, string secret)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var payloadBase64 = Convert.ToBase64String(payloadBytes);
        var secretBytes = Encoding.UTF8.GetBytes(secret);

        using var hmac = new HMACSHA256(secretBytes);
        var signature = hmac.ComputeHash(payloadBytes);
        var signatureBase64 = Convert.ToBase64String(signature);

        return $"{payloadBase64}.{signatureBase64}";
    }

    public static string GetPayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid token format.", nameof(token));
        }
        var payloadBase64 = parts[0];
        try
        {
            var payloadBytes = Convert.FromBase64String(payloadBase64);
            return Encoding.UTF8.GetString(payloadBytes);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid token encoding.", nameof(token));
        }
    }

    public static string GetSignature(string token)
    {
        var parts = token.Split(".");
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid token format.");
        }

        return parts[1];
    }

    public static Result<string> Decode(string token, string secret)
    {
        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            return new Failure([InvalidTokenFormat]);
        }
        var payloadBase64 = parts[0];
        var signatureBase64 = parts[1];
        try
        {
            var payloadBytes = Convert.FromBase64String(payloadBase64);
            var signatureBytes = Convert.FromBase64String(signatureBase64);
            var secretBytes = Encoding.UTF8.GetBytes(secret);
            using (var hmac = new HMACSHA256(secretBytes))
            {
                var computedSignature = hmac.ComputeHash(payloadBytes);
                if (!CryptographicOperations.FixedTimeEquals(signatureBytes, computedSignature))
                {
                    return new Failure([InvalidTokenSignature]);
                }
            }
            return new Success<string>(Encoding.UTF8.GetString(payloadBytes));
        }
        catch (FormatException)
        {
            return new Failure([InvalidTokenEncoding]);
        }
    }
}
