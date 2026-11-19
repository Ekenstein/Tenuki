namespace Tenuki.Protocol;

public interface ITenukiClient
{
    Task GameMessage(GameMessage message);
    Task LobbyMessage(LobbyMessage message);
}
