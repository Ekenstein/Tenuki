using Tenuki.Commons;
using Tenuki.Services.Logic.GameLobby;

namespace Tenuki.Services.Application.GameLobby;

public interface IGameLobbyStore
{
    Task<GameLobbyState?> GetAsync(GameId gameId, CancellationToken cancellationToken);
    Task<bool> CreateAsync(GameLobbyState lobby, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(
        GameLobbyState lobby,
        long expectedRevision,
        CancellationToken cancellationToken
    );
    Task<bool> StartAsync(
        GameLobbyState.Started lobby,
        GameDescription game,
        long expectedRevision,
        CancellationToken cancellationToken
    );
}
