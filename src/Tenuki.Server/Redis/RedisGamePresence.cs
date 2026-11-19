using StackExchange.Redis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Tenuki.Commons;
using Tenuki.Services.Application;

namespace Tenuki.Server.Redis;

internal sealed class RedisGamePresence(
    IConnectionMultiplexer redis,
    IConnectionPresence connectionPresence
) : IGamePresence
{
    private static readonly RedisKey GamesKey = "tenuki:presence:games";

    private static RedisKey GameKey(GameId gameId) => $"tenuki:presence:game:{gameId.Value}";

    public async IAsyncEnumerable<GameId> GetLiveGamesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = redis.GetDatabase();

        var gameIds = await database.SetMembersAsync(GamesKey);

        foreach (var gameIdValue in gameIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var gameId = GameId.Parse(gameIdValue.ToString(), CultureInfo.InvariantCulture);

            var connectionIds = await database.SetMembersAsync(GameKey(gameId));

            foreach (var connectionId in connectionIds)
            {
                if (
                    await connectionPresence.IsActiveAsync(
                        connectionId.ToString(),
                        cancellationToken
                    )
                )
                {
                    yield return gameId;
                    break;
                }
            }
        }
    }

    public async Task JoinGameAsync(
        GameId gameId,
        string connectionId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = redis.GetDatabase();
        var transaction = database.CreateTransaction();

        var subscriberToGame = transaction.SetAddAsync(GameKey(gameId), connectionId);
        var gameAddedToIndex = transaction.SetAddAsync(GamesKey, gameId.Value.ToString());

        var executed = await transaction.ExecuteAsync();
        if (executed)
        {
            await Task.WhenAll(subscriberToGame, gameAddedToIndex);
        }
    }

    public Task LeaveGameAsync(
        GameId gameId,
        string connectionId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = redis.GetDatabase();
        return database.SetRemoveAsync(GameKey(gameId), connectionId);
    }
}
