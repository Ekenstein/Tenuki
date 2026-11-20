using System.Collections.Immutable;
using FsCheck;
using FsCheck.Fluent;
using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Services.Logic;

namespace Tenuki.Services.Tests.Generators;

public static class Arbitraries
{
    private static Gen<GameSettings> GameSettingsGen =>
        from boardSize in Gen.Choose(1, 19)
        let settings = Commons.GameSettings.Default with
        {
            BoardSize = boardSize,
        }
        from handicap in Gen.Elements(settings.ValidHandicaps.ToArray())
        select settings with
        {
            Handicap = handicap,
        };

    public static Arbitrary<GameSettings> GameSettings() => Arb.From(GameSettingsGen);

    private static Gen<InvalidHandicapScenario> InvalidHandicapScenarioGen =>
        from boardSize in Gen.Choose(1, 19)
        let settings = Commons.GameSettings.Default with
        {
            BoardSize = boardSize,
        }
        from handicap in Gen.Choose(-10, 20)
            .Where(value => !settings.ValidHandicaps.Contains(value))
        select new InvalidHandicapScenario(settings with { Handicap = handicap });

    public static Arbitrary<InvalidHandicapScenario> InvalidHandicapScenario() =>
        Arb.From(InvalidHandicapScenarioGen);

    private static Gen<GameState> GameStateGen =>
        from moveCount in Gen.Choose(0, 30)
        from points in Gen.ListOf(BoardPoint().Generator, moveCount)
        select PlayMoves(points);

    private static GameState PlayMoves(IEnumerable<Point> points)
    {
        var now = DateTimeOffset.UnixEpoch;
        var result = GameLogic.CreateGame(GameId.NewId(), Commons.GameSettings.Default, now);
        var state = result.ValueOrThrow(_ => new InvalidOperationException(
            "Default game settings must be valid."
        ));

        return points.Aggregate(state, (state, point) =>
        {
            var command = new GameCommand.Stone(new(), state.NextPlayer, point);
            var outcome = GameLogic.ApplyGameCommand(state, command, now);
            if (outcome is GameCommandOutcome.Applied applied)
            {
                return outcome.State;
            }
            return state;
        });
    }

    private static Gen<Point> LegalPointGen(GameState state)
    {
        var now = DateTimeOffset.UnixEpoch;
        var legalPoints =
            from x in Enumerable.Range(0, state.Settings.BoardSize)
            from y in Enumerable.Range(0, state.Settings.BoardSize)
            let point = new Point(x, y)
            let command = new GameCommand.Stone(new(), state.NextPlayer, point)
            where GameLogic.ApplyGameCommand(state, command, now) is GameCommandOutcome.Applied
            select point;

        return Gen.Elements(legalPoints.ToArray());
    }

    private static Gen<LegalStoneScenario> LegalStoneScenarioGen =>
        from state in GameStateGen
        from point in LegalPointGen(state)
        select new LegalStoneScenario(
            state,
            new GameCommand.Stone(new(), state.NextPlayer, point),
            DateTimeOffset.UnixEpoch
        );

    private static Gen<SuicideScenario> SuicideScenarioGen =>
        from color in Gen.Elements(Enum.GetValues<PlayerColor>())
        let settings = Commons.GameSettings.Default
        from x in Gen.Choose(0, settings.BoardSize - 1)
        from y in Gen.Choose(0, settings.BoardSize - 1)
        let centerPoint = new Point(x, y)
        let surroundingStones = centerPoint
            .GetAdjacentPoints(settings)
            .Select(point => point.ToStone(color))
        let boardPosition = BoardPosition.Empty.AddStones(surroundingStones)
        select new SuicideScenario(boardPosition, centerPoint.ToStone(color.Opponent), settings);

    private static Gen<KoScenario> KoScenarioGen =>
        from color in Gen.Elements(Enum.GetValues<PlayerColor>())
        let settings = Commons.GameSettings.Default
        from x in Gen.Choose(1, settings.BoardSize - 2)
        from y in Gen.Choose(1, settings.BoardSize - 3)
        let point = new Point(x, y)
        let opponent = color.Opponent

        let surroundingStones = new[]
        {
            new Point(x - 1, y).ToStone(opponent),
            new Point(x + 1, y).ToStone(opponent),
            new Point(x, y - 1).ToStone(opponent),
        }

        let supportingStones = new[]
        {
            new Point(x - 1, y + 1).ToStone(color),
            new Point(x + 1, y + 1).ToStone(color),
            new Point(x, y + 2).ToStone(color),
        }

        let capturingPoint = new Point(x, y + 1)
        let now = DateTimeOffset.UnixEpoch
        let initial = GameLogic.CreateGame(GameId.NewId(), settings, now).ValueOrThrow()

        let stones = supportingStones
            .Zip(surroundingStones)
            .SelectMany(pair => new[] { pair.First, pair.Second })
            .Append(point.ToStone(color))

        let history = BuildHistory(stones, initial.NextPlayer)
            .Add(new BoardEvent.Stone(opponent, capturingPoint))

        let state = initial with
        {
            History = history,
            CurrentBoard = history
                .Replay(settings)
                .ValueOrThrow(errors => new InvalidOperationException(
                    $"Failed to replay position, errors: {string.Join(", ", errors.Select(x => x.Code))}"
                )),
        }

        select new KoScenario(state, point.ToStone(color), now);

    private static ImmutableList<BoardEvent> BuildHistory(
        IEnumerable<Stone> stones,
        PlayerColor nextPlayer
    )
    {
        var history = ImmutableList<BoardEvent>.Empty;

        foreach (var stone in stones)
        {
            if (nextPlayer != stone.Color)
            {
                history = history.Add(new BoardEvent.Pass(nextPlayer));
                nextPlayer = nextPlayer.Opponent;
            }

            history = history.Add(new BoardEvent.Stone(stone.Color, stone.Point));

            nextPlayer = nextPlayer.Opponent;
        }

        return history;
    }

    public static Arbitrary<KoScenario> KoScenario() => Arb.From(KoScenarioGen);

    public static Arbitrary<SuicideScenario> SuicideScenario() => Arb.From(SuicideScenarioGen);

    public static Arbitrary<LegalStoneScenario> LegalStoneScenario() =>
        Arb.From(LegalStoneScenarioGen);

    public static Arbitrary<OutOfBoundsPoint> OutOfBoundsPoint()
    {
        var size = Commons.GameSettings.Default.BoardSize;

        var gen =
            from x in Gen.Choose(0, size - 1)
            from y in Gen.Choose(0, size - 1)
            from point in Gen.Elements(
                new Point(-1, y),
                new Point(size, y),
                new Point(x, -1),
                new Point(x, size)
            )
            select new OutOfBoundsPoint(point);

        return Arb.From(gen);
    }

    public static Arbitrary<Point> BoardPoint() =>
        Arb.From(from x in Gen.Choose(0, 18) from y in Gen.Choose(0, 18) select new Point(x, y));

    private static Gen<HandicapScenario> HandicapScenarioGen =>
        from boardSize in Gen.Choose(7, 19)
        let settings = Commons.GameSettings.Default with
        {
            BoardSize = boardSize,
        }
        from handicap in Gen.Elements(settings.ValidHandicaps.ToArray())
        select new HandicapScenario(settings with { Handicap = handicap });

    public static Arbitrary<HandicapScenario> HandicapScenario() => Arb.From(HandicapScenarioGen);
}
