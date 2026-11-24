using Tenuki.Commons;

namespace Tenuki.Services.Application.Game;

public interface IGamePresence
{
    Task JoinGameAsync(GameId gameId, string connectionId, CancellationToken cancellationToken);
    Task LeaveGameAsync(GameId gameId, string connectionId, CancellationToken cancellationToken);
    IAsyncEnumerable<GameId> GetLiveGamesAsync(CancellationToken cancellationToken);
}
