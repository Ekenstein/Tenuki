using System.Collections.Immutable;
using Tenuki.Services.Logic.GameLobby;

namespace Tenuki.Services.Application.GameLobby;

public closed record CreateGameLobbyOutcome
{
    public sealed record Created(GameLobbyState.Waiting GameLobby) : CreateGameLobbyOutcome;

    public sealed record Conflict : CreateGameLobbyOutcome;

    public sealed record Failure(ImmutableList<Error> Errors) : CreateGameLobbyOutcome;
}
