using Tenuki.Commons;
using Tenuki.Services.Application.Game;

namespace Tenuki.Services.Application.GameLobby;

public interface IGameLobbyStore
{
    Task<GameLobbyEntry?> GetAsync(GameId gameId, CancellationToken cancellationToken);
    Task<bool> CreateAsync(GameLobbyEntry lobby, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(
        GameLobbyEntry lobby,
        long expectedRevision,
        CancellationToken cancellationToken
    );
    Task<bool> StartAsync(
        GameLobbyEntry lobby,
        GameDescription game,
        GameOwnership ownership,
        long expectedRevision,
        CancellationToken cancellationToken
    );
}
