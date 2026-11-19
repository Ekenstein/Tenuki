using Tenuki.Server.Actions;

namespace Tenuki.Server.Routes;

public static class GameRoutes
{
    public static void Register(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("games");
        group.MapPost("", GameActions.PostGameAsync);
        group.MapPost("command", GameActions.PostGameCommandAsync);
        group.MapGet("{gameId}", GameActions.GetGameAsync);
        group.MapGet("live", GameActions.GetLiveGamesAsync);
    }
}
