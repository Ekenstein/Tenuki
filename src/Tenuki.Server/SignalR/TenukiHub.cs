using Microsoft.AspNetCore.SignalR;
using Tenuki.Commons;
using Tenuki.Protocol;

namespace Tenuki.Server.SignalR;

public sealed class TenukiHub(
    ConnectionRegistry connectionRegistry,
    GameChannel gameChannel,
    LobbyChannel lobbyChannel,
    GameLobbyChannel gameLobbyChannel
) : Hub<ITenukiClient>
{
    public override async Task OnConnectedAsync()
    {
        await connectionRegistry.AddAsync(Context.ConnectionId, CancellationToken.None);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await connectionRegistry.RemoveAsync(Context.ConnectionId, CancellationToken.None);
        await base.OnDisconnectedAsync(exception);
    }

    public Task JoinLobby(CancellationToken cancellationToken) =>
        lobbyChannel.JoinLobbyAsync(Context.ConnectionId, cancellationToken);

    public Task LeaveLobby(CancellationToken cancellationToken) =>
        lobbyChannel.LeaveLobbyAsync(Context.ConnectionId, cancellationToken);

    public Task JoinGameLobby(GameId gameId, CancellationToken cancellationToken) =>
        gameLobbyChannel.JoinGameLobbyAsync(gameId, Context.ConnectionId, cancellationToken);

    public Task LeaveGameLobby(GameId gameId, CancellationToken cancellationToken) =>
        gameLobbyChannel.LeaveGameLobbyAsync(gameId, Context.ConnectionId, cancellationToken);

    public Task JoinGame(GameId gameId, CancellationToken cancellationToken) =>
        gameChannel.JoinGameAsync(gameId, Context.ConnectionId, cancellationToken);

    public Task LeaveGame(GameId gameId, CancellationToken cancellationToken) =>
        gameChannel.LeaveGameAsync(gameId, Context.ConnectionId, cancellationToken);
}
