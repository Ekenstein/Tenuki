using System.Collections.Immutable;
using Microsoft.AspNetCore.Http.HttpResults;
using Tenuki.Commons;
using Tenuki.Server.Http.Models;
using Tenuki.Services.Application.GameLobby;
using Tenuki.Services.Logic.GameLobby;

namespace Tenuki.Server.Http.Actions;

public static class GameLobbyActions
{
    public static async Task<
        Results<CreatedAtRoute<GameLobbyState.Waiting>, BadRequest<ImmutableList<Error>>, Conflict>
    > PostGameLobbyAsync(
        GameLobbySettings request,
        GameLobbyService service,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = await service.CreateGameLobbyAsync(request, cancellationToken);
        return outcome switch
        {
            CreateGameLobbyOutcome.Created created => TypedResults.CreatedAtRoute(
                created.GameLobby,
                routeName: Endpoints.GetGameLobby,
                routeValues: new { gameId = created.GameLobby.GameId }
            ),
            CreateGameLobbyOutcome.Conflict => TypedResults.Conflict(),
            CreateGameLobbyOutcome.Failure failure => TypedResults.BadRequest(failure.Errors),
        };
    }

    public static async Task<Results<Ok<GameLobbyState>, NotFound<GameId>>> GetGameLobbyAsync(
        GameId gameId,
        GameLobbyService service,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var gameLobby = await service.GetGameLobbyAsync(gameId, cancellationToken);
        return gameLobby switch
        {
            null => TypedResults.NotFound(gameId),
            _ => TypedResults.Ok(gameLobby),
        };
    }

    public static async Task<
        Results<
            Ok<PostGameChallengeResponse>,
            BadRequest<ImmutableList<Error>>,
            NotFound<GameId>,
            Conflict
        >
    > PostChallengeAsync(
        GameId gameId,
        GameLobbyService service,
        GameLobbySettings challenge,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = await service.AddChallengeAsync(gameId, challenge, cancellationToken);
        return outcome switch
        {
            AddChallengeOutcome.Added added => TypedResults.Ok(
                new PostGameChallengeResponse(added.ChallengeId)
            ),
            AddChallengeOutcome.Conflict => TypedResults.Conflict(),
            AddChallengeOutcome.NotFound notFound => TypedResults.NotFound(notFound.GameId),
            AddChallengeOutcome.Failure failure => TypedResults.BadRequest(failure.Errors),
        };
    }

    public static async Task<
        Results<NoContent, BadRequest<ImmutableList<Error>>, NotFound<GameId>, Conflict>
    > DeleteChallengeAsync(
        GameId gameId,
        ChallengeId challengeId,
        GameLobbyService service,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = await service.RejectChallengeAsync(gameId, challengeId, cancellationToken);
        return outcome switch
        {
            RejectChallengeOutcome.Applied => TypedResults.NoContent(),
            RejectChallengeOutcome.Conflict => TypedResults.Conflict(),
            RejectChallengeOutcome.NotFound notFound => TypedResults.NotFound(notFound.GameId),
            RejectChallengeOutcome.Failure failure => TypedResults.BadRequest(failure.Errors),
        };
    }

    public static async Task<
        Results<
            CreatedAtRoute<GameDescription>,
            BadRequest<ImmutableList<Error>>,
            NotFound<GameId>,
            Conflict
        >
    > PostAcceptChallengeAsync(
        GameId gameId,
        ChallengeId challengeId,
        GameLobbyService service,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = await service.AcceptChallengeAsync(gameId, challengeId, cancellationToken);
        return outcome switch
        {
            AcceptChallengeOutcome.Created created => TypedResults.CreatedAtRoute(
                created.Game,
                routeName: Endpoints.GetGame,
                routeValues: new { gameId = created.Game.Id }
            ),
            AcceptChallengeOutcome.Conflict => TypedResults.Conflict(),
            AcceptChallengeOutcome.NotFound notFound => TypedResults.NotFound(notFound.GameId),
            AcceptChallengeOutcome.Failure failure => TypedResults.BadRequest(failure.Errors),
        };
    }
}
