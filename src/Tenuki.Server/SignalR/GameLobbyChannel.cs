using Microsoft.AspNetCore.SignalR;
using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Server.Messaging;

namespace Tenuki.Server.SignalR;

public sealed class GameLobbyChannel(IHubContext<TenukiHub, ITenukiClient> hubContext)
    : IMessageBroadcaster<GameLobbyMessage>
{
    private static string GroupName(GameId gameId) => $"tenuki:game-lobby:{gameId}";

    public Task BroadcastAsync(GameLobbyMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return hubContext.Clients.Groups(GroupName(message.GameId)).GameLobbyMessage(message);
    }

    public Task JoinGameLobbyAsync(
        GameId gameId,
        string connectionId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return hubContext.Groups.AddToGroupAsync(
            connectionId,
            GroupName(gameId),
            cancellationToken
        );
    }

    public Task LeaveGameLobbyAsync(
        GameId gameId,
        string connectionId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return hubContext.Groups.RemoveFromGroupAsync(
            connectionId,
            GroupName(gameId),
            cancellationToken
        );
    }
}
