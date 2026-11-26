using Tenuki.Commons;

namespace Tenuki.Services.Application.GameLobby;

public sealed record GameLobbyEntry(GameLobbyState State, GameLobbyOwnership Ownership);
