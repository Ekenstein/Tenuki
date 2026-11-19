using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Immutable;
using Tenuki.Commons;
using Tenuki.Server.Models;
using Tenuki.Services.Application;

namespace Tenuki.Server.Actions;

public static class GameActions
{
    public static async Task<
        Results<Ok<GameDescription>, BadRequest<ImmutableList<Error>>>
    > PostGameAsync(
        ActionContext context,
        PostGameRequest request,
        CancellationToken cancellationToken
    )
    {
        var gameSettings = new GameSettings(
            BoardSize: request.BoardSize,
            Komi: request.Komi,
            Handicap: request.Handicap,
            request.TimeSettings
        );

        var result = await context.Games.CreateGameAsync(gameSettings, cancellationToken);

        return result switch
        {
            Success<GameDescription> success => TypedResults.Ok(success.Value),
            Failure failure => TypedResults.BadRequest(failure.Errors),
        };
    }

    public static async Task<
        Results<Ok<GameCommandResponse>, BadRequest<ImmutableList<Error>>>
    > PostGameCommandAsync(
        ActionContext context,
        PostGameCommandRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await context.Games.ApplyGameCommandAsync(
            request.StateToken,
            request.Command,
            cancellationToken
        );

        return result switch
        {
            Success<GameCommandResponse> success => TypedResults.Ok(success.Value),
            Failure failure => TypedResults.BadRequest(failure.Errors),
        };
    }

    public static async Task<Results<Ok<GameDescription>, NotFound<GameId>>> GetGameAsync(
        ActionContext context,
        GameId gameId,
        CancellationToken cancellationToken
    )
    {
        var game = await context.Games.GetGameAsync(gameId, cancellationToken);
        return game != null ? TypedResults.Ok(game) : TypedResults.NotFound(gameId);
    }

    public static async Task<Ok<GetGamesResponse>> GetLiveGamesAsync(
        ActionContext context,
        CancellationToken cancellationToken
    )
    {
        var games = await context
            .Games.GetLiveGamesAsync(cancellationToken)
            .ToArrayAsync(cancellationToken);
        return TypedResults.Ok(new GetGamesResponse([.. games]));
    }
}
