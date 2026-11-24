using System.Text.Json;
using StackExchange.Redis;
using Tenuki.Services.Logic.GameLobby;

namespace Tenuki.Server.Redis;

public static class RedisGameLobbyState
{
    public static readonly RedisValue GameIdEntryName = "gameId";
    public static readonly RedisValue RevisionEntryName = "revision";
    private static readonly RedisValue MetadataEntryName = "metadata";

    public static HashEntry[] ToEntries(
        this GameLobbyState state,
        JsonSerializerOptions jsonOptions
    ) =>
        [
            new(GameIdEntryName, state.GameId.ToString()),
            new(RevisionEntryName, state.Revision),
            new(MetadataEntryName, JsonSerializer.Serialize(state, jsonOptions)),
        ];

    public static GameLobbyState FromEntries(HashEntry[] entries, JsonSerializerOptions jsonOptions)
    {
        var valuesByName = entries.ToDictionary(x => x.Name, x => x.Value);
        var metadata = valuesByName[MetadataEntryName].ToString();
        return JsonSerializer.Deserialize<GameLobbyState>(metadata, jsonOptions)
            ?? throw new InvalidOperationException("Invalid entries for game lobby state.");
    }
}
