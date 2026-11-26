using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Services.Application.GameLobby;

public closed record RemoveChallengeOutcome
{
    public sealed record NotFound(GameId GameId) : RemoveChallengeOutcome;

    public sealed record Conflict : RemoveChallengeOutcome;

    public sealed record Applied : RemoveChallengeOutcome;

    public sealed record Failure(ImmutableList<Error> Errors) : RemoveChallengeOutcome;

    public sealed record Forbidden : RemoveChallengeOutcome;
}
