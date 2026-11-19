using FsCheck;
using FsCheck.Xunit;
using Tenuki.Tests.Utils;

namespace Tenuki.Utils.Tests;

public class HmacTokenCodecTests
{
    [Property]
    public void Encode_Then_Decode_Returns_Original_Payload(string payload, string secret)
    {
        var token = HmacTokenCodec.Encode(payload, secret);
        var result = HmacTokenCodec.Decode(token, secret);
        var decodedPayloed = result.AssertNoFailure();
        Assert.Equal(payload, decodedPayloed);
    }

    [Property]
    public void Encoding_Is_Deterministic(string payload, string secret)
    {
        var t1 = HmacTokenCodec.Encode(payload, secret);
        var t2 = HmacTokenCodec.Encode(payload, secret);
        Assert.Equal(t1, t2);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Different_Secrets_Produce_Different_Tokens(string payload, DifferentStrings secrets)
    {
        var token1 = HmacTokenCodec.Encode(payload, secrets.Value1);
        var token2 = HmacTokenCodec.Encode(payload, secrets.Value2);

        Assert.NotEqual(token1, token2);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Decode_With_Wrong_Secret_Throws(string payload, DifferentStrings secrets)
    {
        var token = HmacTokenCodec.Encode(payload, secrets.Value1);
        var result = HmacTokenCodec.Decode(token, secrets.Value2);
        Assert.False(result.IsSuccess);
    }

    [Property]
    public void Tampered_Payload_Throws(string payload, string secret)
    {
        var token = HmacTokenCodec.Encode(payload, secret);
        var parts = token.Split('.');

        if (parts[0].Length == 0)
            return;

        var tamperedPayload =
            parts[0][0] == 'A' ? parts[0][1..].Insert(0, "B") : parts[0][1..].Insert(0, "A");

        var tamperedToken = $"{tamperedPayload}.{parts[1]}";

        var result = HmacTokenCodec.Decode(tamperedToken, secret);
        Assert.False(result.IsSuccess);
    }

    [Property]
    public void Tampered_Signature_Throws(string payload, string secret)
    {
        var token = HmacTokenCodec.Encode(payload, secret);
        var parts = token.Split('.');

        if (parts[1].Length == 0)
            return;

        var tamperedSignature =
            parts[1][0] == 'A' ? parts[1][1..].Insert(0, "B") : parts[1][1..].Insert(0, "A");

        var tamperedToken = $"{parts[0]}.{tamperedSignature}";

        var result = HmacTokenCodec.Decode(tamperedToken, secret);
        Assert.False(result.IsSuccess);
    }

    [Property]
    public void Token_Has_Correct_Format(string payload, string secret)
    {
        var token = HmacTokenCodec.Encode(payload, secret);
        var parts = token.Split('.');

        Assert.Equal(2, parts.Length);
        Assert.Equal(payload, HmacTokenCodec.GetPayload(token));
    }
}
