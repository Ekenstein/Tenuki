using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Services.Application.GameLobby;

public closed record RejectChallengeOutcome
{
    public sealed record NotFound(GameId GameId) : RejectChallengeOutcome;

    public sealed record Conflict : RejectChallengeOutcome;

    public sealed record Applied : RejectChallengeOutcome;

    public sealed record Failure(ImmutableList<Error> Errors) : RejectChallengeOutcome;
}
