namespace Tenuki.Services.Application.Game;

internal static class GameServiceErrors
{
    internal static readonly Error GameNotFound = new(nameof(GameNotFound), "Game not found");
}
