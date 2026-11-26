using System.Collections.Immutable;
using Microsoft.AspNetCore.Http.HttpResults;
using Tenuki.Commons;
using Tenuki.Server.Http.Bindings;
using Tenuki.Server.Http.Models;
using Tenuki.Services.Application.GameLobby;

namespace Tenuki.Server.Http.Actions;

public static class GameLobbyActions
{
    public static async Task<
        Results<CreatedAtRoute<PostGameLobbyResponse>, BadRequest<ImmutableList<Error>>, Conflict>
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
                new PostGameLobbyResponse(created.Id, created.OwnerToken.ToHexString()),
                routeName: Endpoints.GetGameLobby,
                routeValues: new { gameId = created.Id }
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
                new PostGameChallengeResponse(added.ChallengeId, added.OwnerToken.ToHexString())
            ),
            AddChallengeOutcome.Conflict => TypedResults.Conflict(),
            AddChallengeOutcome.NotFound notFound => TypedResults.NotFound(notFound.GameId),
            AddChallengeOutcome.Failure failure => TypedResults.BadRequest(failure.Errors),
        };
    }

    public static async Task<
        Results<
            NoContent,
            BadRequest<ImmutableList<Error>>,
            NotFound<GameId>,
            Conflict,
            UnauthorizedHttpResult,
            StatusCodeHttpResult
        >
    > DeleteChallengeAsync(
        GameId gameId,
        ChallengeId challengeId,
        OwnershipCredentials? credentials,
        GameLobbyService service,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (credentials == null)
        {
            return TypedResults.Unauthorized();
        }

        var outcome = await service.RemoveChallengeAsync(
            gameId,
            challengeId,
            credentials.Token,
            cancellationToken
        );
        return outcome switch
        {
            RemoveChallengeOutcome.Applied => TypedResults.NoContent(),
            RemoveChallengeOutcome.Conflict => TypedResults.Conflict(),
            RemoveChallengeOutcome.NotFound notFound => TypedResults.NotFound(notFound.GameId),
            RemoveChallengeOutcome.Failure failure => TypedResults.BadRequest(failure.Errors),
            RemoveChallengeOutcome.Forbidden => TypedResults.StatusCode(
                StatusCodes.Status403Forbidden
            ),
        };
    }

    public static async Task<
        Results<
            CreatedAtRoute<GameDescription>,
            BadRequest<ImmutableList<Error>>,
            NotFound<GameId>,
            Conflict,
            UnauthorizedHttpResult,
            StatusCodeHttpResult
        >
    > PostAcceptChallengeAsync(
        GameId gameId,
        ChallengeId challengeId,
        GameLobbyService service,
        OwnershipCredentials? credentials,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (credentials == null)
        {
            return TypedResults.Unauthorized();
        }

        var outcome = await service.AcceptChallengeAsync(
            gameId,
            challengeId,
            credentials.Token,
            cancellationToken
        );
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
            AcceptChallengeOutcome.Forbidden => TypedResults.StatusCode(
                StatusCodes.Status403Forbidden
            ),
        };
    }
}
