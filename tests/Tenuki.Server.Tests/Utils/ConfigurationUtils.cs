using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Tenuki.Server.Tests.Utils;

internal static class ConfigurationUtils
{
    internal static ImmutableDictionary<string, string?> ToConfiguration<T>(
        this T options,
        string sectionName
    )
    {
        var utf8Bytes = JsonSerializer.SerializeToUtf8Bytes(
            new Dictionary<string, T> { [sectionName] = options }
        );

        using var ms = new MemoryStream(utf8Bytes);

        var configuration = new ConfigurationBuilder().AddJsonStream(ms).Build();

        return configuration.AsEnumerable().ToImmutableDictionary(x => x.Key, x => x.Value);
    }
}
