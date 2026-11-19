using StackExchange.Redis;

namespace Tenuki.Server.Redis;

public sealed record RedisStreamSettings(RedisKey StreamKey, RedisValue ConsumerGroup)
{
    public static readonly RedisStreamSettings Default = new(
        RedisConstants.StreamKey,
        RedisConstants.ConsumerGroup
    );
}
