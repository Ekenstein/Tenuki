using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Services.Application.GameLobby;

public closed record AcceptChallengeOutcome
{
    public sealed record Failure(ImmutableList<Error> Errors) : AcceptChallengeOutcome;

    public sealed record NotFound(GameId GameId) : AcceptChallengeOutcome;

    public sealed record Created(GameDescription Game) : AcceptChallengeOutcome;

    public sealed record Conflict : AcceptChallengeOutcome;

    public sealed record Forbidden : AcceptChallengeOutcome;
}
