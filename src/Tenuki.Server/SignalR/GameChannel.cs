using Microsoft.AspNetCore.SignalR;
using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Server.Messaging;
using Tenuki.Services.Application;

namespace Tenuki.Server.SignalR;

public sealed class GameChannel(
    IHubContext<TenukiHub, ITenukiClient> hubContext,
    IGamePresence presence
) : IMessageBroadcaster<GameMessage>
{
    private static string GroupName(GameId gameId) => $"tenuki:game:{gameId.Value}";

    public async Task JoinGameAsync(
        GameId gameId,
        string connectionId,
        CancellationToken cancellationToken
    )
    {
        await hubContext.Groups.AddToGroupAsync(connectionId, GroupName(gameId), cancellationToken);

        await presence.JoinGameAsync(gameId, connectionId, cancellationToken);
    }

    public async Task LeaveGameAsync(
        GameId gameId,
        string connectionId,
        CancellationToken cancellationToken
    )
    {
        await hubContext.Groups.RemoveFromGroupAsync(
            connectionId,
            GroupName(gameId),
            cancellationToken
        );
        await presence.LeaveGameAsync(gameId, connectionId, cancellationToken);
    }

    public Task BroadcastAsync(GameMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return hubContext.Clients.Group(GroupName(message.GameId)).GameMessage(message);
    }
}
