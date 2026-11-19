using Microsoft.AspNetCore.SignalR;
using Tenuki.Protocol;
using Tenuki.Server.Messaging;

namespace Tenuki.Server;

public sealed class LobbyChannel(IHubContext<TenukiHub, ITenukiClient> hubContext)
    : IMessageBroadcaster<LobbyMessage>
{
    private const string GroupName = "tenuki:lobby";

    public Task JoinLobbyAsync(string connectionId, CancellationToken cancellationToken) =>
        hubContext.Groups.AddToGroupAsync(connectionId, GroupName, cancellationToken);

    public Task LeaveLobbyAsync(string connectionId, CancellationToken cancellationToken) =>
        hubContext.Groups.RemoveFromGroupAsync(connectionId, GroupName, cancellationToken);

    public Task BroadcastAsync(LobbyMessage lobbyMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return hubContext.Clients.Group(GroupName).LobbyMessage(lobbyMessage);
    }
}
