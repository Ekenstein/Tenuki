using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tenuki.Utils;

internal sealed class IdJsonConverter<TId> : JsonConverter<TId>
    where TId : Id<TId>
{
    public override TId? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var str = reader.GetString();
        return Id<TId>.TryParse(str, null, out var id) ? id : null;
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}
