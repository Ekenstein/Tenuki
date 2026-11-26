using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Services.Application.GameLobby;

public closed record CreateGameLobbyOutcome
{
    public sealed record Created(GameId Id, SecretToken OwnerToken) : CreateGameLobbyOutcome;

    public sealed record Conflict : CreateGameLobbyOutcome;

    public sealed record Failure(ImmutableList<Error> Errors) : CreateGameLobbyOutcome;
}
