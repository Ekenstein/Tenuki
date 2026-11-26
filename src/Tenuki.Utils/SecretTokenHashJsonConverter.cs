using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tenuki.Utils;

internal sealed class SecretTokenHashJsonConverter : JsonConverter<SecretTokenHash>
{
    public override SecretTokenHash Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        SecretTokenHash.TryParse(reader.GetString(), null, out var hash)
            ? hash
            : throw new JsonException("Invalid ownership token hash.");

    public override void Write(
        Utf8JsonWriter writer,
        SecretTokenHash value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue(value.ToHexString());
}
