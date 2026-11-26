using Tenuki.Commons;
using Tenuki.Services.Application.Game;
using Tenuki.Services.Logic.Game;
using Tenuki.Services.Logic.GameLobby;

namespace Tenuki.Services.Application.GameLobby;

public sealed class GameLobbyService(
    IGameLobbyStore store,
    IGameStateTokenCodec codec,
    IIdGenerator idGenerator,
    Random random,
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

        var ownerToken = SecretToken.Generate();
        var entry = new GameLobbyEntry(lobby, new(ownerToken.ComputeHash(), []));

        return !await store.CreateAsync(entry, cancellationToken)
            ? new CreateGameLobbyOutcome.Conflict()
            : new CreateGameLobbyOutcome.Created(gameId, ownerToken);
    }

    public async Task<GameLobbyState?> GetGameLobbyAsync(
        GameId gameId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entry = await store.GetAsync(gameId, cancellationToken);
        return entry?.State;
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

        var challengeId = ChallengeId.NewId(idGenerator);
        var challenge = new GameChallenge(challengeId, proposedSettings);

        var result = GameLobbyLogic.AddChallenge(lobby.State, challenge);
        if (!result.TryGetValue(out var challengeAdded))
        {
            return new AddChallengeOutcome.Failure(result.Errors);
        }

        var ownerToken = SecretToken.Generate();

        var updatedLobby = lobby with
        {
            State = challengeAdded,
            Ownership = lobby.Ownership.AddChallenger(challengeId, ownerToken),
        };

        return !await store.UpdateAsync(updatedLobby, lobby.State.Revision, cancellationToken)
            ? new AddChallengeOutcome.Conflict()
            : new AddChallengeOutcome.Added(challenge.Id, ownerToken);
    }

    public async Task<RemoveChallengeOutcome> RemoveChallengeAsync(
        GameId gameId,
        ChallengeId challengeId,
        SecretToken ownerToken,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lobby = await store.GetAsync(gameId, cancellationToken);
        if (lobby == null)
        {
            return new RemoveChallengeOutcome.NotFound(gameId);
        }

        if (
            !lobby.Ownership.IsOwner(ownerToken)
            && !lobby.Ownership.IsChallengeOwner(challengeId, ownerToken)
        )
        {
            return new RemoveChallengeOutcome.Forbidden();
        }

        var result = GameLobbyLogic.RemoveChallenge(lobby.State, challengeId);
        if (!result.TryGetValue(out var challengeRejected))
        {
            return new RemoveChallengeOutcome.Failure(result.Errors);
        }

        var updatedLobby = lobby with
        {
            State = challengeRejected,
            Ownership = lobby.Ownership.RemoveChallenger(challengeId),
        };

        return !await store.UpdateAsync(updatedLobby, lobby.State.Revision, cancellationToken)
            ? new RemoveChallengeOutcome.Conflict()
            : new RemoveChallengeOutcome.Applied();
    }

    public async Task<AcceptChallengeOutcome> AcceptChallengeAsync(
        GameId gameId,
        ChallengeId challengeId,
        SecretToken ownerToken,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var lobby = await store.GetAsync(gameId, cancellationToken);
        if (lobby == null)
        {
            return new AcceptChallengeOutcome.NotFound(gameId);
        }

        if (!lobby.Ownership.IsOwner(ownerToken))
        {
            return new AcceptChallengeOutcome.Forbidden();
        }

        var accepted = GameLobbyLogic.AcceptChallenge(lobby.State, challengeId);
        if (!accepted.TryGetValue(out var started))
        {
            return new AcceptChallengeOutcome.Failure(accepted.Errors);
        }

        if (!lobby.Ownership.Challengers.TryGetValue(challengeId, out var challengerTokenHash))
        {
            throw new InvalidOperationException($"Missing ownership for challenge '{challengeId}'");
        }

        var updatedLobby = lobby with
        {
            State = started,
            Ownership = lobby.Ownership.ClearChallengers(),
        };
        var gameOwnership = CreateGameOwnership(
            started.Settings.ColorPreference,
            lobby.Ownership.Owner,
            challengerTokenHash
        );

        return await StartGameAsync(
            updatedLobby,
            gameOwnership,
            lobby.State.Revision,
            cancellationToken
        );
    }

    private GameOwnership CreateGameOwnership(
        ColorPreference colorPreference,
        SecretTokenHash ownerTokenHash,
        SecretTokenHash challengerTokenHash
    ) =>
        colorPreference switch
        {
            ColorPreference.OwnerTakesBlack => new GameOwnership(
                ownerTokenHash,
                challengerTokenHash
            ),
            ColorPreference.OwnerTakesWhite => new GameOwnership(
                challengerTokenHash,
                ownerTokenHash
            ),
            ColorPreference.Random => random.Next(2) == 0
                ? new GameOwnership(ownerTokenHash, challengerTokenHash)
                : new GameOwnership(challengerTokenHash, ownerTokenHash),
            _ => throw new ArgumentOutOfRangeException(
                nameof(colorPreference),
                $"Could not recognize the color preference '{colorPreference}'"
            ),
        };

    private async Task<AcceptChallengeOutcome> StartGameAsync(
        GameLobbyEntry gameLobby,
        GameOwnership gameOwnership,
        long previousRevision,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = GameLogic.CreateGame(
            gameLobby.State.GameId,
            gameLobby.State.Settings.GameSettings,
            timeProvider.GetUtcNow()
        );
        if (!result.TryGetValue(out var game))
        {
            return new AcceptChallengeOutcome.Failure(result.Errors);
        }

        var encodedState = codec.Encode(game);
        var description = new GameDescription(game.Id, encodedState, game.Revision);

        return !await store.StartAsync(
            gameLobby,
            description,
            gameOwnership,
            previousRevision,
            cancellationToken
        )
            ? new AcceptChallengeOutcome.Conflict()
            : new AcceptChallengeOutcome.Created(description);
    }
}
