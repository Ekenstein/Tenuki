using System.Collections.Immutable;

namespace Tenuki.Commons;

public sealed record GameState(
    GameId Id,
    long Revision,
    GameSettings Settings,
    ClockState Clock,
    GameResult? Result,
    ImmutableList<BoardEvent> History,
    BoardPosition CurrentBoard,
    ulong? PreviousPositionHash,
    PlayerColor? UndoRequestedBy,
    CountingState? Counting
);
