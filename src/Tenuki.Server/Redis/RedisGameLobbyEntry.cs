using System.Text.Json;
using StackExchange.Redis;
using Tenuki.Commons;
using Tenuki.Services.Application.GameLobby;

namespace Tenuki.Server.Redis;

public static class RedisGameLobbyEntry
{
    public static readonly RedisValue GameIdEntryName = "gameId";
    public static readonly RedisValue RevisionEntryName = "revision";
    private static readonly RedisValue MetadataEntryName = "metadata";
    private static readonly RedisValue OwnershipEntryName = "ownership";

    public static HashEntry[] ToEntries(
        this GameLobbyEntry entry,
        JsonSerializerOptions jsonOptions
    ) =>
        [
            new(GameIdEntryName, entry.State.GameId.ToString()),
            new(RevisionEntryName, entry.State.Revision),
            new(MetadataEntryName, JsonSerializer.Serialize(entry.State, jsonOptions)),
            new(OwnershipEntryName, JsonSerializer.Serialize(entry.Ownership, jsonOptions)),
        ];

    public static GameLobbyEntry FromEntries(HashEntry[] entries, JsonSerializerOptions jsonOptions)
    {
        var valuesByName = entries.ToDictionary(x => x.Name, x => x.Value);
        var metadata = valuesByName[MetadataEntryName].ToString();
        var ownership = valuesByName[OwnershipEntryName].ToString();

        return new(
            JsonSerializer.Deserialize<GameLobbyState>(metadata, jsonOptions)
                ?? throw new InvalidOperationException("Invalid entries for game lobby state."),
            JsonSerializer.Deserialize<GameLobbyOwnership>(ownership, jsonOptions)
                ?? throw new InvalidOperationException("Invalid entries for game lobby ownership.")
        );
    }
}
