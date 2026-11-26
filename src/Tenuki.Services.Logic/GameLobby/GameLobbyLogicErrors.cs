namespace Tenuki.Services.Logic.GameLobby;

public static class GameLobbyLogicErrors
{
    public static readonly Error GameAlreadyStarted = new(
        nameof(GameAlreadyStarted),
        "The game has already started."
    );
    public static readonly Error ChallengeNotFound = new(
        nameof(ChallengeNotFound),
        "The challenge does not exist."
    );
    public static readonly Error ChallengeAlreadyExist = new(
        nameof(ChallengeAlreadyExist),
        "The challenge already exist."
    );
}
