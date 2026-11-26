using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Tenuki.Commons;
using Tenuki.Server.Messaging;
using Tenuki.Services.Application.Game;
using Tenuki.Services.Application.GameLobby;

namespace Tenuki.Server.Redis;

public sealed class RedisGameLobbyStore(
    IConnectionMultiplexer redis,
    RedisStreamSettings streamSettings,
    IOptions<JsonOptions> jsonOptions
) : IGameLobbyStore
{
    public async Task<bool> CreateAsync(GameLobbyEntry lobby, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var transaction = db.CreateTransaction();

        var key = RedisConstants.GameLobbyKey(lobby.State.GameId);
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

    public async Task<GameLobbyEntry?> GetAsync(GameId gameId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var hashEntries = await db.HashGetAllAsync(RedisConstants.GameLobbyKey(gameId))
            .WaitAsync(cancellationToken);
        if (hashEntries.Length <= 0)
        {
            return null;
        }

        return RedisGameLobbyEntry.FromEntries(hashEntries, jsonOptions.Value.SerializerOptions);
    }

    public async Task<bool> UpdateAsync(
        GameLobbyEntry lobby,
        long expectedRevision,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var transaction = db.CreateTransaction();

        var updatedGameLobby = UpdateGameLobbyAsync(
            transaction,
            lobby,
            expectedRevision,
            cancellationToken
        );

        if (!await transaction.ExecuteAsync().WaitAsync(cancellationToken))
        {
            return false;
        }

        await updatedGameLobby.WaitAsync(cancellationToken);
        return true;
    }

    private Task UpdateGameLobbyAsync(
        ITransaction transaction,
        GameLobbyEntry lobby,
        long expectedRevision,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = RedisConstants.GameLobbyKey(lobby.State.GameId);
        var keyExist = Condition.KeyExists(key);
        var revisionEqualsExpectedRevision = Condition.HashEqual(
            key,
            RedisGameLobbyEntry.RevisionEntryName,
            expectedRevision
        );
        transaction.AddCondition(keyExist);
        transaction.AddCondition(revisionEqualsExpectedRevision);

        return Task.WhenAll(
            transaction.QueueAsync(
                new IncomingMessage.GameLobbyChanged(lobby.State),
                streamSettings,
                jsonOptions.Value.SerializerOptions,
                cancellationToken
            ),
            transaction.HashSetAsync(key, lobby.ToEntries(jsonOptions.Value.SerializerOptions))
        );
    }

    public async Task<bool> StartAsync(
        GameLobbyEntry lobby,
        GameDescription game,
        GameOwnership ownership,
        long expectedRevision,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = redis.GetDatabase();
        var transaction = db.CreateTransaction();

        var gameKey = RedisConstants.GameKey(lobby.State.GameId);

        var gameKeyDoesNotExistCondition = Condition.KeyNotExists(gameKey);

        transaction.AddCondition(gameKeyDoesNotExistCondition);

        var updateGameLobby = UpdateGameLobbyAsync(
            transaction,
            lobby,
            expectedRevision,
            cancellationToken
        );
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
