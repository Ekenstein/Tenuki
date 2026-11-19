using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Immutable;
using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Server.Messaging;
using Tenuki.Services.Application;

namespace Tenuki.Server.Redis;

public sealed class RedisGameStateStore(
    IConnectionMultiplexer connection,
    RedisStreamSettings streamSettings,
    IOptions<JsonOptions> jsonOptions
) : IGameStateStore
{
    public async Task<bool> CreateAsync(GameDescription game, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = connection.GetDatabase();
        var transaction = db.CreateTransaction();
        var gameKey = RedisConstants.GameKey(game.Id);
        transaction.AddCondition(Condition.KeyNotExists(gameKey));

        return await CommitAsync(
            transaction,
            AddOrUpdateGameAsync(transaction, gameKey, game),
            transaction.QueueAsync(
                new IncomingMessage.GameAdded(game),
                streamSettings,
                jsonOptions.Value.SerializerOptions,
                cancellationToken
            )
        );
    }

    public async Task<GameDescription?> GetAsync(GameId gameId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = connection.GetDatabase();
        var hashEntries = await db.HashGetAllAsync(RedisConstants.GameKey(gameId));
        return hashEntries.ToGameDescription().ValueOrDefault();
    }

    public async Task<GameStateUpdateOutcome> UpdateAsync(
        GameDescription game,
        long expectedRevision,
        ImmutableList<GameCommand> committedCommands,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var gameKey = RedisConstants.GameKey(game.Id);

        var db = connection.GetDatabase();
        var transaction = db.CreateTransaction();

        transaction.AddCondition(
            Condition.HashEqual(gameKey, RedisGameDescription.RevisionEntryName, expectedRevision)
        );

        var result = await CommitAsync(
            transaction,
            AddOrUpdateGameAsync(transaction, gameKey, game),
            transaction.QueueAsync(
                new IncomingMessage.GameChanged(game, committedCommands),
                streamSettings,
                jsonOptions.Value.SerializerOptions,
                cancellationToken
            )
        );

        if (!result)
        {
            return await GetAsync(game.Id, cancellationToken) switch
            {
                { } gameState => new GameStateUpdateOutcome.Conflict(
                    gameState.StateToken,
                    gameState.Revision
                ),
                _ => new GameStateUpdateOutcome.NotFound(),
            };
        }

        return new GameStateUpdateOutcome.Updated();
    }

    private static Task AddOrUpdateGameAsync(
        ITransaction transaction,
        RedisKey gameKey,
        GameDescription game
    )
    {
        return transaction.HashSetAsync(gameKey, game.ToEntries());
    }

    private static async Task<bool> CommitAsync(ITransaction transaction, params Task[] tasks)
    {
        if (await transaction.ExecuteAsync())
        {
            await Task.WhenAll(tasks);
            return true;
        }
        return false;
    }
}
