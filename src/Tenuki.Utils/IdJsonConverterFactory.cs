using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tenuki.Utils;

internal sealed class IdJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        try
        {
            typeof(Id<>).MakeGenericType(typeToConvert);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override JsonConverter? CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var converter = typeof(IdJsonConverter<>).MakeGenericType(typeToConvert);
        return Activator.CreateInstance(converter) as JsonConverter;
    }
}
