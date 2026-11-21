namespace Tenuki.Services.Logic.GameLobby;

internal static class GameLobbyErrors
{
    internal static readonly Error GameAlreadyStarted = new(
        nameof(GameAlreadyStarted),
        "The game has already started."
    );
    internal static readonly Error ChallengeNotFound = new(
        nameof(ChallengeNotFound),
        "The challenge does not exist."
    );
    internal static readonly Error ChallengeAlreadyExist = new(
        nameof(ChallengeAlreadyExist),
        "The challenge already exist."
    );
}
