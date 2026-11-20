using System.Text.Json;
using StackExchange.Redis;
using Tenuki.Server.Messaging;

namespace Tenuki.Server.Redis;

public sealed record IncomingMessageEnvelope(IncomingMessage Message)
{
    private static readonly RedisValue MessageEntryName = "message";

    public IEnumerable<NameValueEntry> ToEntries(JsonSerializerOptions jsonOptions)
    {
        var json = JsonSerializer.Serialize(Message, jsonOptions);
        yield return new(MessageEntryName, json);
    }

    public static Result<IncomingMessageEnvelope> FromEntries(
        IEnumerable<NameValueEntry> entries,
        JsonSerializerOptions jsonOptions
    )
    {
        var valuesByName = entries.ToDictionary(x => x.Name, x => x.Value);
        if (!valuesByName.TryGetValue(MessageEntryName, out var messageEntry))
        {
            return new Failure([RedisErrors.NoMessageEntry]);
        }

        var message = JsonSerializer.Deserialize<IncomingMessage>(
            messageEntry.ToString(),
            jsonOptions
        );

        if (message == null)
        {
            return new Failure([RedisErrors.InvalidMessageFormat]);
        }

        return new Success<IncomingMessageEnvelope>(new(message));
    }
}
