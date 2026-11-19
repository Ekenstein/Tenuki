using System.Runtime.CompilerServices;
using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Services.Logic;

namespace Tenuki.Services.Application;

public sealed class GameService(
    IGameStateStore store,
    IGameStateTokenCodec tokens,
    IGamePresence presence,
    TimeProvider timeProvider
)
{
    public Task<GameDescription?> GetGameAsync(GameId gameId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return store.GetAsync(gameId, cancellationToken);
    }

    public async IAsyncEnumerable<GameDescription> GetLiveGamesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await foreach (var gameId in presence.GetLiveGamesAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var game = await store.GetAsync(gameId, cancellationToken);
            if (game is not null)
            {
                yield return game;
            }
        }
    }

    public async Task<Result<GameDescription>> CreateGameAsync(
        GameSettings gameSettings,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = GameLogic.CreateGame(gameSettings, timeProvider.GetUtcNow());

        if (result.TryGetValue(out var state))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stateToken = tokens.Encode(state);
            var gameDescription = new GameDescription(state.Id, stateToken, state.Revision);
            if (!await store.CreateAsync(gameDescription, cancellationToken))
            {
                throw new InvalidOperationException("There's another game with the same id.");
            }

            return Result.Success(gameDescription);
        }

        return new Failure(result.Errors);
    }

    public async Task<Result<GameCommandResponse>> ApplyGameCommandAsync(
        string stateToken,
        GameCommand command,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var decodeResult = tokens.Decode(stateToken);
        if (!decodeResult.TryGetValue(out var gameState))
        {
            return new Failure(decodeResult.Errors);
        }

        var commandOutcome = GameLogic.ApplyGameCommand(
            gameState,
            command,
            timeProvider.GetUtcNow()
        );

        var signedState = tokens.Encode(commandOutcome.State);

        var updateOutcome = await store.UpdateAsync(
            game: new(commandOutcome.State.Id, signedState, commandOutcome.State.Revision),
            expectedRevision: gameState.Revision,
            committedCommands: commandOutcome is GameCommandOutcome.Applied ? [command] : [],
            cancellationToken: cancellationToken
        );

        return updateOutcome switch
        {
            GameStateUpdateOutcome.NotFound => new Failure([GameServiceErrors.GameNotFound]),
            GameStateUpdateOutcome.Conflict conflict => new Success<GameCommandResponse>(
                new GameCommandResponse.Conflict(
                    conflict.CurrentStateToken,
                    conflict.CurrentRevision
                )
            ),
            GameStateUpdateOutcome.Updated => Result.Success(ToGameCommandResponse(commandOutcome)),
        };
    }

    private GameCommandResponse ToGameCommandResponse(GameCommandOutcome outcome)
    {
        var stateToken = tokens.Encode(outcome.State);
        return outcome switch
        {
            GameCommandOutcome.Applied applied => new GameCommandResponse.Applied(
                stateToken,
                applied.State.Revision
            ),
            GameCommandOutcome.Rejected rejected => new GameCommandResponse.Rejected(
                stateToken,
                rejected.State.Revision,
                rejected.Errors
            ),
            GameCommandOutcome.TimeExpired timeExpired => new GameCommandResponse.TimeExpired(
                stateToken,
                timeExpired.State.Revision
            ),
        };
    }
}
