using Tenuki.Server.Http.Actions;

namespace Tenuki.Server.Http.Routes;

public static class GameLobbyRoutes
{
    public static void Register(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("game-lobbies");

        group.MapPost("", GameLobbyActions.PostGameLobbyAsync);
        group
            .MapGet("{gameId}", GameLobbyActions.GetGameLobbyAsync)
            .WithName(Endpoints.GetGameLobby);
        group.MapPost("{gameId}/challenge", GameLobbyActions.PostChallengeAsync);
        group.MapDelete("{gameId}/challenge/{challengeId}", GameLobbyActions.DeleteChallengeAsync);
        group.MapPost(
            "{gameId}/challenge/{challengeId}/accept",
            GameLobbyActions.PostAcceptChallengeAsync
        );
    }
}
