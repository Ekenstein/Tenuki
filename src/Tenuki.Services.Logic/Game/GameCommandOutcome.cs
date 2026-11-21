using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Services.Logic.Game;

public closed record GameCommandOutcome(GameState State)
{
    public sealed record Applied(GameState State) : GameCommandOutcome(State);

    public sealed record Rejected(GameState State, ImmutableList<Error> Errors)
        : GameCommandOutcome(State);

    public sealed record TimeExpired(GameState State) : GameCommandOutcome(State);
}
