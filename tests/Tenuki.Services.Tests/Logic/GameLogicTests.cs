using FsCheck.Xunit;
using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Services.Logic;
using Tenuki.Services.Tests.Generators;
using Xunit;

namespace Tenuki.Services.Tests.Logic;

public sealed class GameLogicTests
{
    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Successful_Stone_Move_Alternates_Player(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        Assert.Equal(scenario.State.NextPlayer.Opponent, applied.State.NextPlayer);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Successful_Stone_Move_Places_Stone(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        Assert.Contains(applied.State.CurrentBoard.Position, stone =>
            stone.Point == scenario.Command.Point && stone.Color == scenario.Command.Color
        );
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Successful_Stone_Move_Preserves_Game_Identity(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        Assert.Equal(scenario.State.Id, applied.State.Id);
        Assert.Equal(scenario.State.Settings, applied.State.Settings);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Placing_A_Stone_On_Opponents_Turn_Is_Rejected(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);
        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        var rejected = Assert.IsType<GameCommandOutcome.Rejected>(
            GameLogic.ApplyGameCommand(applied.State, scenario.Command, scenario.Now)
        );
        Assert.Contains(GameLogicErrors.NotPlayerTurn, rejected.Errors);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Successful_Stone_Move_Increments_Revision(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);
        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);
        Assert.Equal(scenario.State.Revision + 1, applied.State.Revision);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Placing_Stone_On_Occupied_Point_Is_Rejected(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        var nextCommand = scenario.Command with { Color = applied.State.NextPlayer };

        var secondOutcome = GameLogic.ApplyGameCommand(applied.State, nextCommand, scenario.Now);

        var rejected = Assert.IsType<GameCommandOutcome.Rejected>(secondOutcome);
        Assert.Contains(GameLogicErrors.PointOccupied, rejected.Errors);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Successful_Stone_Move_Adds_At_Most_One_Stone(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        var before = scenario.State.CurrentBoard.Position.Count;
        var after = applied.State.CurrentBoard.Position.Count;

        Assert.True(
            after <= before + 1,
            $"Expected at most {before + 1} stones, but found {after}."
        );
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Existing_Stones_Never_Change_Color(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        var before = scenario.State.CurrentBoard.Position;
        var after = applied.State.CurrentBoard.Position;

        foreach (var stone in before)
        {
            var oppositeStone = stone with { Color = stone.Color.Opponent };

            Assert.DoesNotContain(oppositeStone, after);
        }
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Captures_Never_Decrease(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        var before = scenario.State.CurrentBoard;
        var after = applied.State.CurrentBoard;

        Assert.True(after.BlackCaptures >= before.BlackCaptures);
        Assert.True(after.WhiteCaptures >= before.WhiteCaptures);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Successful_Stone_Move_Never_Increases_Opponent_Captures(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        var before = scenario.State.CurrentBoard;
        var after = applied.State.CurrentBoard;

        switch (scenario.Command.Color)
        {
            case PlayerColor.Black:
                Assert.Equal(before.WhiteCaptures, after.WhiteCaptures);
                break;

            case PlayerColor.White:
                Assert.Equal(before.BlackCaptures, after.BlackCaptures);
                break;
        }
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Stone_Outside_Board_Is_Rejected(OutOfBoundsPoint point)
    {
        var now = DateTimeOffset.UnixEpoch;

        var result = GameLogic.CreateGame(GameId.NewId(), GameSettings.Default, now);

        Assert.True(result.TryGetValue(out var state));

        var command = new GameCommand.Stone(new(), state.NextPlayer, point.Value);

        var outcome = GameLogic.ApplyGameCommand(state, command, now);

        var rejected = Assert.IsType<GameCommandOutcome.Rejected>(outcome);

        Assert.Contains(GameLogicErrors.StoneOutOfBounds, rejected.Errors);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Successful_Stone_Move_Is_Always_Within_Board(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        var size = scenario.State.Settings.BoardSize;

        Assert.All(applied.State.CurrentBoard.Position, stone =>
        {
            Assert.InRange(stone.Point.X, 0, size - 1);
            Assert.InRange(stone.Point.Y, 0, size - 1);
        });
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Removed_Stones_Are_Accounted_For(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        var before = scenario.State.CurrentBoard;
        var after = applied.State.CurrentBoard;

        var removed = before.Position.Except(after.Position);

        var removedBlack = removed.Count(s => s.Color == PlayerColor.Black);
        var removedWhite = removed.Count(s => s.Color == PlayerColor.White);

        Assert.Equal(removedBlack, after.WhiteCaptures - before.WhiteCaptures);

        Assert.Equal(removedWhite, after.BlackCaptures - before.BlackCaptures);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Successful_Stone_Move_Preserves_Placed_Stone(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        Assert.Contains(
            new Stone(scenario.Command.Color, scenario.Command.Point),
            applied.State.CurrentBoard.Position
        );
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Successful_Stone_Move_Never_Repeats_Position(LegalStoneScenario scenario)
    {
        var outcome = GameLogic.ApplyGameCommand(scenario.State, scenario.Command, scenario.Now);

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        if (scenario.State.GetPreviousPosition() is { } previousPosition)
        {
            var currentPosition = applied.State.CurrentBoard.Position;
            Assert.False(previousPosition.Position.SetEquals(currentPosition));
        }
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Immediate_Ko_Recapture_Is_Rejected(KoScenario scenario)
    {
        var command = new GameCommand.Stone(new(), scenario.Stone.Color, scenario.Stone.Point);

        var outcome = GameLogic.ApplyGameCommand(scenario.State, command, scenario.Now);

        var rejected = Assert.IsType<GameCommandOutcome.Rejected>(outcome);

        Assert.Contains(GameLogicErrors.PositionIsRepeating, rejected.Errors);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Resigning_Always_Makes_Opponent_The_Winner(LegalStoneScenario scenario)
    {
        var state = scenario.State;
        var resigningPlayer = state.NextPlayer;

        var outcome = GameLogic.ApplyGameCommand(
            state,
            new GameCommand.Resign(new(), resigningPlayer),
            scenario.Now
        );

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        var result = Assert.IsType<GameResult.Resign>(applied.State.Result);

        Assert.Equal(resigningPlayer.Opponent, result.Winner);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Resigning_Never_Changes_Board(LegalStoneScenario scenario)
    {
        var state = scenario.State;

        var outcome = GameLogic.ApplyGameCommand(
            state,
            new GameCommand.Resign(new(), state.NextPlayer),
            scenario.Now
        );

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        Assert.Equal(state.CurrentBoard, applied.State.CurrentBoard);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Resigning_Prevents_Further_Stone_Moves(LegalStoneScenario scenario)
    {
        var state = scenario.State;

        var resign = GameLogic.ApplyGameCommand(
            state,
            new GameCommand.Resign(new(), state.NextPlayer),
            scenario.Now
        );

        var resignedState = Assert.IsType<GameCommandOutcome.Applied>(resign).State;

        var outcome = GameLogic.ApplyGameCommand(resignedState, scenario.Command, scenario.Now);

        var rejected = Assert.IsType<GameCommandOutcome.Rejected>(outcome);

        Assert.Contains(GameLogicErrors.GameAlreadyFinished, rejected.Errors);
        Assert.Equal(resignedState, rejected.State);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Resigning_Prevents_Further_Passes(LegalStoneScenario scenario)
    {
        var state = scenario.State;

        var resign = GameLogic.ApplyGameCommand(
            state,
            new GameCommand.Resign(new(), state.NextPlayer),
            scenario.Now
        );

        var resignedState = Assert.IsType<GameCommandOutcome.Applied>(resign).State;

        var outcome = GameLogic.ApplyGameCommand(
            resignedState,
            new GameCommand.Pass(new(), scenario.Command.Color),
            scenario.Now
        );

        var rejected = Assert.IsType<GameCommandOutcome.Rejected>(outcome);

        Assert.Contains(GameLogicErrors.GameAlreadyFinished, rejected.Errors);
        Assert.Equal(resignedState, rejected.State);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Resigning_Increments_Revision_Exactly_Once(LegalStoneScenario scenario)
    {
        var state = scenario.State;

        var outcome = GameLogic.ApplyGameCommand(
            state,
            new GameCommand.Resign(new(), state.NextPlayer),
            scenario.Now
        );

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        Assert.Equal(state.Revision + 1, applied.State.Revision);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Resigning_Preserves_Game_Identity_And_Settings(LegalStoneScenario scenario)
    {
        var state = scenario.State;

        var outcome = GameLogic.ApplyGameCommand(
            state,
            new GameCommand.Resign(new(), state.NextPlayer),
            scenario.Now
        );

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);

        Assert.Equal(state.Id, applied.State.Id);
        Assert.Equal(state.Settings, applied.State.Settings);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Player_Can_Resign_When_Its_Not_Their_Turn(LegalStoneScenario scenario)
    {
        var state = scenario.State;
        var resigningPlayer = state.NextPlayer.Opponent;

        var outcome = GameLogic.ApplyGameCommand(
            state,
            new GameCommand.Resign(new(), resigningPlayer),
            scenario.Now
        );

        var applied = Assert.IsType<GameCommandOutcome.Applied>(outcome);
        var result = Assert.IsType<GameResult.Resign>(applied.State.Result);

        Assert.Equal(state.NextPlayer, result.Winner);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Ko_Recapture_Is_Legal_After_Two_Passes(KoScenario scenario)
    {
        var state = scenario.State;
        var now = scenario.Now;

        // The player who wants to recapture passes.
        var firstPass = GameLogic.ApplyGameCommand(
            state,
            new GameCommand.Pass(new(), state.NextPlayer),
            now
        );

        var afterFirstPass = Assert.IsType<GameCommandOutcome.Applied>(firstPass).State;

        // The opponent passes.
        var secondPass = GameLogic.ApplyGameCommand(
            afterFirstPass,
            new GameCommand.Pass(new(), afterFirstPass.NextPlayer),
            now
        );

        var afterSecondPass = Assert.IsType<GameCommandOutcome.Applied>(secondPass).State;

        // The original player attempts the recapture.
        var recapture = GameLogic.ApplyGameCommand(
            afterSecondPass,
            new GameCommand.Stone(new(), scenario.Stone.Color, scenario.Stone.Point),
            now
        );

        Assert.IsType<GameCommandOutcome.Applied>(recapture);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Two_Consecutive_Passes_Initiate_Counting(LegalStoneScenario scenario)
    {
        var state = scenario.State;

        var firstPass = GameLogic.ApplyGameCommand(
            state,
            new GameCommand.Pass(new(), state.NextPlayer),
            scenario.Now
        );

        var applied = Assert.IsType<GameCommandOutcome.Applied>(firstPass);

        var secondPass = GameLogic.ApplyGameCommand(
            applied.State,
            new GameCommand.Pass(new(), applied.State.NextPlayer),
            scenario.Now
        );

        Assert.NotNull(Assert.IsType<GameCommandOutcome.Applied>(secondPass).State.Counting);
    }
}
