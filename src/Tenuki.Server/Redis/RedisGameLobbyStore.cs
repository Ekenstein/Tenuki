using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Tenuki.Commons;
using Tenuki.Server.Messaging;
using Tenuki.Services.Application.GameLobby;
using Tenuki.Services.Logic.GameLobby;

namespace Tenuki.Server.Redis;

public sealed class RedisGameLobbyStore(
    IConnectionMultiplexer redis,
    RedisStreamSettings streamSettings,
    IOptions<JsonOptions> jsonOptions
) : IGameLobbyStore
{
    public async Task<bool> CreateAsync(GameLobbyState lobby, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var transaction = db.CreateTransaction();

        var key = RedisConstants.GameLobbyKey(lobby.GameId);
        var gameKeyDoesNotExist = Condition.KeyNotExists(key);

        transaction.AddCondition(gameKeyDoesNotExist);
        var addGame = transaction.HashSetAsync(
            key,
            lobby.ToEntries(jsonOptions.Value.SerializerOptions)
        );

        if (!await transaction.ExecuteAsync().WaitAsync(cancellationToken))
        {
            return false;
        }

        await addGame.WaitAsync(cancellationToken);
        return true;
    }

    public async Task<GameLobbyState?> GetAsync(GameId gameId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var hashEntries = await db.HashGetAllAsync(RedisConstants.GameLobbyKey(gameId))
            .WaitAsync(cancellationToken);
        if (hashEntries.Length <= 0)
        {
            return null;
        }

        return RedisGameLobbyState.FromEntries(hashEntries, jsonOptions.Value.SerializerOptions);
    }

    public async Task<bool> UpdateAsync(
        GameLobbyState lobby,
        long expectedRevision,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var transaction = db.CreateTransaction();

        var updatedGameLobby = UpdateGameLobbyAsync(transaction, lobby, expectedRevision);

        if (!await transaction.ExecuteAsync().WaitAsync(cancellationToken))
        {
            return false;
        }

        await updatedGameLobby.WaitAsync(cancellationToken);
        return true;
    }

    private Task UpdateGameLobbyAsync(
        ITransaction transaction,
        GameLobbyState lobby,
        long expectedRevision
    )
    {
        var key = RedisConstants.GameLobbyKey(lobby.GameId);
        var keyExist = Condition.KeyExists(key);
        var revisionEqualsExpectedRevision = Condition.HashEqual(
            key,
            RedisGameLobbyState.RevisionEntryName,
            expectedRevision
        );
        transaction.AddCondition(keyExist);
        transaction.AddCondition(revisionEqualsExpectedRevision);

        return transaction.HashSetAsync(key, lobby.ToEntries(jsonOptions.Value.SerializerOptions));
    }

    public Task<bool> StartAsync(
        GameLobbyState.Started lobby,
        GameDescription game,
        long expectedRevision,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = redis.GetDatabase();
        var transaction = db.CreateTransaction();

        return StartAsync(transaction, lobby, game, expectedRevision, cancellationToken);
    }

    private async Task<bool> StartAsync(
        ITransaction transaction,
        GameLobbyState.Started lobby,
        GameDescription game,
        long expectedRevision,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var gameKey = RedisConstants.GameKey(lobby.GameId);

        var gameKeyDoesNotExistCondition = Condition.KeyNotExists(gameKey);

        transaction.AddCondition(gameKeyDoesNotExistCondition);

        var updateGameLobby = UpdateGameLobbyAsync(transaction, lobby, expectedRevision);
        var addGame = transaction.HashSetAsync(gameKey, game.ToEntries());
        var notifyGameAdded = transaction.QueueAsync(
            new IncomingMessage.GameAdded(game),
            streamSettings,
            jsonOptions.Value.SerializerOptions,
            cancellationToken
        );

        if (await transaction.ExecuteAsync().WaitAsync(cancellationToken))
        {
            await Task.WhenAll(addGame, updateGameLobby, notifyGameAdded)
                .WaitAsync(cancellationToken);
            return true;
        }

        return false;
    }
}
