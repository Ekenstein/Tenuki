using Tenuki.Commons;
using Tenuki.Services.Application.Game;
using Tenuki.Services.Logic.Game;
using Tenuki.Services.Logic.GameLobby;

namespace Tenuki.Services.Application.GameLobby;

public sealed class GameLobbyService(
    IGameLobbyStore store,
    IGameStateTokenCodec codec,
    IIdGenerator idGenerator,
    TimeProvider timeProvider
)
{
    public async Task<CreateGameLobbyOutcome> CreateGameLobbyAsync(
        GameLobbySettings settings,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var gameId = GameId.NewId(idGenerator);
        var result = GameLobbyLogic.CreateLobby(gameId, settings);
        if (!result.TryGetValue(out var lobby))
        {
            return new CreateGameLobbyOutcome.Failure(result.Errors);
        }

        return !await store.CreateAsync(lobby, cancellationToken)
            ? new CreateGameLobbyOutcome.Conflict()
            : new CreateGameLobbyOutcome.Created(lobby);
    }

    public Task<GameLobbyState?> GetGameLobbyAsync(
        GameId gameId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return store.GetAsync(gameId, cancellationToken);
    }

    public async Task<AddChallengeOutcome> AddChallengeAsync(
        GameId gameId,
        GameLobbySettings proposedSettings,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var lobby = await store.GetAsync(gameId, cancellationToken);
        if (lobby == null)
        {
            return new AddChallengeOutcome.NotFound(gameId);
        }

        var challenge = new GameChallenge(ChallengeId.NewId(idGenerator), proposedSettings);

        var result = GameLobbyLogic.AddChallenge(lobby, challenge);
        if (!result.TryGetValue(out var challengeAdded))
        {
            return new AddChallengeOutcome.Failure(result.Errors);
        }

        return !await store.UpdateAsync(challengeAdded, lobby.Revision, cancellationToken)
            ? new AddChallengeOutcome.Conflict()
            : new AddChallengeOutcome.Added(challenge.Id);
    }

    public async Task<RejectChallengeOutcome> RejectChallengeAsync(
        GameId gameId,
        ChallengeId challengeId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lobby = await store.GetAsync(gameId, cancellationToken);
        if (lobby == null)
        {
            return new RejectChallengeOutcome.NotFound(gameId);
        }

        var result = GameLobbyLogic.RejectChallenge(lobby, challengeId);
        if (!result.TryGetValue(out var challengeRejected))
        {
            return new RejectChallengeOutcome.Failure(result.Errors);
        }

        return !await store.UpdateAsync(challengeRejected, lobby.Revision, cancellationToken)
            ? new RejectChallengeOutcome.Conflict()
            : new RejectChallengeOutcome.Applied();
    }

    public async Task<AcceptChallengeOutcome> AcceptChallengeAsync(
        GameId gameId,
        ChallengeId challengeId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var gameLobby = await store.GetAsync(gameId, cancellationToken);
        if (gameLobby == null)
        {
            return new AcceptChallengeOutcome.NotFound(gameId);
        }

        var accepted = GameLobbyLogic.AcceptChallenge(gameLobby, challengeId);
        if (!accepted.TryGetValue(out var started))
        {
            return new AcceptChallengeOutcome.Failure(accepted.Errors);
        }

        return await StartGameAsync(started, gameLobby.Revision, cancellationToken);
    }

    private async Task<AcceptChallengeOutcome> StartGameAsync(
        GameLobbyState.Started startedGame,
        long previousRevision,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = GameLogic.CreateGame(
            startedGame.GameId,
            startedGame.Settings.GameSettings,
            timeProvider.GetUtcNow()
        );
        if (!result.TryGetValue(out var game))
        {
            return new AcceptChallengeOutcome.Failure(result.Errors);
        }

        var encodedState = codec.Encode(game);
        var description = new GameDescription(game.Id, encodedState, game.Revision);

        return !await store.StartAsync(
            startedGame,
            description,
            previousRevision,
            cancellationToken
        )
            ? new AcceptChallengeOutcome.Conflict()
            : new AcceptChallengeOutcome.Created(description);
    }
}
