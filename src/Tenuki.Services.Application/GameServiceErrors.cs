namespace Tenuki.Services.Application;

internal static class GameServiceErrors
{
    internal static readonly Error GameNotFound = new(nameof(GameNotFound), "Game not found");
}
