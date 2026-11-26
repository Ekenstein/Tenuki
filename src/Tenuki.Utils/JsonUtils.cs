using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tenuki.Utils;

public static class JsonUtils
{
    public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions().Apply(
        ConfigureOptions
    );
    public static readonly JsonSerializerOptions PrettyDefaultOptions = DefaultOptions.Apply(x =>
        x.WriteIndented = true
    );

    public static void ConfigureOptions(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new IdJsonConverterFactory());
        options.Converters.Add(new SecretTokenHashJsonConverter());
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }
}
