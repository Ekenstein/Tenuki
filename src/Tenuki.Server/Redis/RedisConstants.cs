using StackExchange.Redis;
using Tenuki.Commons;

namespace Tenuki.Server.Redis;

internal static class RedisConstants
{
    internal static readonly RedisKey StreamKey = "tenuki:notifications";
    internal static readonly RedisValue ConsumerGroup = "tenuki-broadcaster";
    internal static readonly RedisValue GameKeyPrefix = "tenuki:game:";
    internal static readonly RedisValue GameLobbyKeyPrefix = "tenuki:game-lobby:";

    internal static RedisKey GameKey(GameId gameId) => $"{GameKeyPrefix}{gameId}";

    internal static RedisKey GameLobbyKey(GameId gameId) => $"{GameLobbyKeyPrefix}{gameId}";
}
