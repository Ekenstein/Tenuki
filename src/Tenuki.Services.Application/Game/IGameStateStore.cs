using System.Collections.Immutable;
using Tenuki.Commons;
using Tenuki.Protocol;

namespace Tenuki.Services.Application.Game;

public closed record GameStateUpdateOutcome
{
    public sealed record Updated : GameStateUpdateOutcome;

    public sealed record Conflict(string CurrentStateToken, long CurrentRevision)
        : GameStateUpdateOutcome;

    public sealed record NotFound : GameStateUpdateOutcome;
}

public interface IGameStateStore
{
    /// <summary>
    /// Atomically commits a new game state if the expected revision matches.
    /// A successful commit also schedules a state change notification.
    /// </summary>
    Task<GameStateUpdateOutcome> UpdateAsync(
        GameDescription game,
        long expectedRevision,
        ImmutableList<GameCommand> committedCommands,
        CancellationToken cancellationToken
    );

    Task<GameDescription?> GetAsync(GameId gameId, CancellationToken cancellationToken);
}
