using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Services.Application.GameLobby;

public closed record AddChallengeOutcome
{
    public sealed record NotFound(GameId GameId) : AddChallengeOutcome;

    public sealed record Conflict : AddChallengeOutcome;

    public sealed record Added(ChallengeId ChallengeId) : AddChallengeOutcome;

    public sealed record Failure(ImmutableList<Error> Errors) : AddChallengeOutcome;
}
