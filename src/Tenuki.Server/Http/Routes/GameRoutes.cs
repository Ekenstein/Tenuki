using Tenuki.Server.Http.Actions;

namespace Tenuki.Server.Http.Routes;

public static class GameRoutes
{
    public static void Register(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("games");
        group.MapPost("command", GameActions.PostGameCommandAsync);
        group.MapGet("{gameId}", GameActions.GetGameAsync).WithName(Endpoints.GetGame);
        group.MapGet("live", GameActions.GetLiveGamesAsync);
    }
}
