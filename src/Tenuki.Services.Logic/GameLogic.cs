using Tenuki.Commons;
using Tenuki.Protocol;

namespace Tenuki.Services.Logic;

public static class GameLogic
{
    public static Result<GameState> CreateGame(GameId id, GameSettings settings, DateTimeOffset now)
    {
        var validationErrors = settings.Validate();
        if (validationErrors.Any())
        {
            return new Failure([.. validationErrors]);
        }

        var gameState = new GameState(
            Id: id,
            Revision: 1,
            Settings: settings,
            Clock: settings.TimeSettings.GetInitialClockState(now),
            Result: null,
            History: [],
            CurrentBoard: settings.InitialBoardPosition,
            PreviousPositionHash: null,
            UndoRequestedBy: null,
            Counting: null
        );

        return new Success<GameState>(gameState);
    }

    public static GameCommandOutcome ApplyGameCommand(
        GameState state,
        GameCommand command,
        DateTimeOffset now
    )
    {
        if (state.Result is not null)
        {
            return new GameCommandOutcome.Rejected(state, [GameLogicErrors.GameAlreadyFinished]);
        }

        var updatedState = state.UpdateClock(now);
        if (updatedState.IsTimeOver)
        {
            var gameOver = updatedState with
            {
                Result = new GameResult.Time(command.Color.Opponent),
            };
            return new GameCommandOutcome.TimeExpired(gameOver.IncreaseRevision());
        }

        return command switch
        {
            GameCommand.Pass pass => Pass(updatedState, pass),
            GameCommand.Stone stone => PlaceStone(updatedState, stone),
            GameCommand.Resign resign => Resign(updatedState, resign),
            GameCommand.RequestUndo requestUndo => RequestUndo(updatedState, requestUndo),
            GameCommand.AcceptUndo acceptUndo => AcceptUndo(updatedState, acceptUndo),
            GameCommand.RejectUndo rejectUndo => RejectUndo(updatedState, rejectUndo),
            GameCommand.MarkDeadStones markDeadStones => MarkDeadStones(
                updatedState,
                markDeadStones
            ),
            GameCommand.UnmarkDeadStones unmarkDeadStones => UnmarkDeadStones(
                updatedState,
                unmarkDeadStones
            ),
            GameCommand.AcceptScore acceptScore => AcceptScore(updatedState, acceptScore),
            GameCommand.ResumePlay resumePlay => ResumePlay(updatedState, resumePlay),
        };
    }

    private static GameCommandOutcome ResumePlay(GameState state, GameCommand.ResumePlay _)
    {
        if (state.Counting == null)
        {
            return new GameCommandOutcome.Rejected(state, [GameLogicErrors.CountingNotInitiated]);
        }

        var updatedState = state with { Counting = null };

        return new GameCommandOutcome.Applied(updatedState.IncreaseRevision());
    }

    private static GameCommandOutcome AcceptScore(
        GameState state,
        GameCommand.AcceptScore acceptScore
    )
    {
        if (state.Counting == null)
        {
            return new GameCommandOutcome.Rejected(state, [GameLogicErrors.CountingNotInitiated]);
        }

        var updatedState = state with { Counting = state.Counting.AcceptScore(acceptScore.Color) };

        if (updatedState.Counting.ScoreFinished)
        {
            var position = updatedState.CurrentBoard.RemoveStones(updatedState.Counting.DeadStones);
            var score = position.GetScore(state.Settings);

            var finished = updatedState with
            {
                Result = new GameResult.Score(score.Points, score.Color),
            };

            return new GameCommandOutcome.Applied(finished.IncreaseRevision());
        }

        return new GameCommandOutcome.Applied(updatedState.IncreaseRevision());
    }

    private static GameCommandOutcome MarkDeadStones(
        GameState state,
        GameCommand.MarkDeadStones markDeadStones
    )
    {
        if (state.Counting == null)
        {
            return new GameCommandOutcome.Rejected(state, [GameLogicErrors.CountingNotInitiated]);
        }

        var updatedState = state with
        {
            Counting = state.Counting.MarkDeadStones(markDeadStones.Points),
        };

        return new GameCommandOutcome.Applied(updatedState.IncreaseRevision());
    }

    private static GameCommandOutcome UnmarkDeadStones(
        GameState state,
        GameCommand.UnmarkDeadStones unmarkDeadStones
    )
    {
        if (state.Counting == null)
        {
            return new GameCommandOutcome.Rejected(state, [GameLogicErrors.CountingNotInitiated]);
        }

        var updatedState = state with
        {
            Counting = state.Counting.UnmarkDeadStones(unmarkDeadStones.Points),
        };

        return new GameCommandOutcome.Applied(updatedState.IncreaseRevision());
    }

    private static GameCommandOutcome RejectUndo(GameState state, GameCommand.RejectUndo rejectUndo)
    {
        if (state.UndoRequestedBy == null)
        {
            return new GameCommandOutcome.Rejected(state, [GameLogicErrors.NoUndoRequest]);
        }

        if (state.UndoRequestedBy == rejectUndo.Color)
        {
            return new GameCommandOutcome.Rejected(
                state,
                [GameLogicErrors.NotAuthorizedToAcceptUndo]
            );
        }

        var updatedState = state with { UndoRequestedBy = null };

        return new GameCommandOutcome.Applied(updatedState.IncreaseRevision());
    }

    private static GameCommandOutcome AcceptUndo(GameState state, GameCommand.AcceptUndo undo)
    {
        if (state.UndoRequestedBy == null)
        {
            return new GameCommandOutcome.Rejected(state, [GameLogicErrors.NoUndoRequest]);
        }

        if (state.UndoRequestedBy == undo.Color)
        {
            return new GameCommandOutcome.Rejected(
                state,
                [GameLogicErrors.NotAuthorizedToAcceptUndo]
            );
        }

        var index = state.History.FindLastIndex(x => x.Color == state.UndoRequestedBy);
        if (index < 0)
        {
            return new GameCommandOutcome.Rejected(state, [GameLogicErrors.NothingToUndo]);
        }

        var remainingHistory = state.History.RemoveRange(index, state.History.Count - index);
        var previousPosition = remainingHistory
            .InitOrNull()
            ?.Replay(state.Settings)
            .ValueOrThrow(CorruptHistory);
        var currentPosition = remainingHistory.Replay(state.Settings).ValueOrThrow(CorruptHistory);

        var updatedState = state with
        {
            History = remainingHistory,
            PreviousPositionHash = previousPosition?.Hash,
            CurrentBoard = currentPosition,
            UndoRequestedBy = null,
        };

        return new GameCommandOutcome.Applied(updatedState.IncreaseRevision());

        static InvalidOperationException CorruptHistory(IEnumerable<Error> errors)
        {
            var errorCodes = string.Join(", ", errors.Select(x => x.Code));
            return new($"The game state has a corrupt history. The errors were '{errorCodes}'.");
        }
    }

    private static GameCommandOutcome RequestUndo(GameState state, GameCommand.RequestUndo undo)
    {
        if (!state.History.Any(x => x.Color == undo.Color))
        {
            return new GameCommandOutcome.Rejected(state, [GameLogicErrors.NothingToUndo]);
        }

        var updatedState = state with { UndoRequestedBy = undo.Color };

        return new GameCommandOutcome.Applied(updatedState.IncreaseRevision());
    }

    private static GameCommandOutcome.Applied Resign(GameState state, GameCommand.Resign resign)
    {
        var updatedState = state with { Result = new GameResult.Resign(resign.Color.Opponent) };

        return new(updatedState.CompleteTurn().IncreaseRevision());
    }

    private static GameCommandOutcome PlaceStone(GameState state, GameCommand.Stone stone)
    {
        if (state.NextPlayer != stone.Color)
        {
            return new GameCommandOutcome.Rejected(state, [GameLogicErrors.NotPlayerTurn]);
        }

        var result =
            from updatedPosition in state.CurrentBoard.PlaceStone(
                new(stone.Color, stone.Point),
                state.Settings
            )
            from validatedPosition in ValidateBoardPosition(state, updatedPosition)
            let updatedState = state with
            {
                History = state.History.Add(new BoardEvent.Stone(stone.Color, stone.Point)),
                CurrentBoard = validatedPosition,
            }
            select updatedState.CompleteTurn().IncreaseRevision();

        return result switch
        {
            Success<GameState> success => new GameCommandOutcome.Applied(success.Value),
            Failure failure => new GameCommandOutcome.Rejected(state, failure.Errors),
        };
    }

    private static GameCommandOutcome Pass(GameState state, GameCommand.Pass pass)
    {
        if (state.NextPlayer != pass.Color)
        {
            return new GameCommandOutcome.Rejected(state, [GameLogicErrors.NotPlayerTurn]);
        }

        var updatedState = state with
        {
            History = state.History.Add(new BoardEvent.Pass(pass.Color)),
        };

        var lastEvt = state.History.LastOrDefault();
        if (lastEvt is BoardEvent.Pass)
        {
            return new GameCommandOutcome.Applied(
                updatedState.InitiateCounting().CompleteTurn().IncreaseRevision()
            );
        }

        return new GameCommandOutcome.Applied(updatedState.CompleteTurn().IncreaseRevision());
    }

    private static Result<BoardPosition> ValidateBoardPosition(
        GameState state,
        BoardPosition newPosition
    )
    {
        var previousPosition = state.PreviousPositionHash;

        if (previousPosition != null && newPosition.Hash == previousPosition)
        {
            return new Failure([GameLogicErrors.PositionIsRepeating]);
        }

        return Result.Success(newPosition);
    }
}
