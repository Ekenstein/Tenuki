namespace Tenuki.Services.Application.GameLobby;

public static class GameLobbyServiceErrors
{
    public static readonly Error ChallengerNotFound = new(
        nameof(ChallengerNotFound),
        "The challenger of the challenge could not be found."
    );
}
