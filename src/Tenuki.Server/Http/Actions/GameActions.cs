using System.Collections.Immutable;
using Microsoft.AspNetCore.Http.HttpResults;
using Tenuki.Commons;
using Tenuki.Server.Http.Models;
using Tenuki.Services.Application.Game;

namespace Tenuki.Server.Http.Actions;

public static class GameActions
{
    public static async Task<
        Results<Ok<GameCommandResponse>, BadRequest<ImmutableList<Error>>>
    > PostGameCommandAsync(
        GameService service,
        PostGameCommandRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await service.ApplyGameCommandAsync(
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
        GameService service,
        GameId gameId,
        CancellationToken cancellationToken
    )
    {
        var game = await service.GetGameAsync(gameId, cancellationToken);
        return game != null ? TypedResults.Ok(game) : TypedResults.NotFound(gameId);
    }

    public static async Task<Ok<GetGamesResponse>> GetLiveGamesAsync(
        GameService service,
        CancellationToken cancellationToken
    )
    {
        var games = await service
            .GetLiveGamesAsync(cancellationToken)
            .ToArrayAsync(cancellationToken);
        return TypedResults.Ok(new GetGamesResponse([.. games]));
    }
}
