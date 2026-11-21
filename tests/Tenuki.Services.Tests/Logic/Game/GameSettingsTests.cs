using FsCheck;
using FsCheck.Xunit;
using Tenuki.Commons;
using Tenuki.Services.Logic.Game;
using Tenuki.Services.Tests.Generators;
using Xunit;

namespace Tenuki.Services.Tests.Logic.Game;

public sealed class GameSettingsTests
{
    [Property]
    public void Non_Positive_Board_Size_Is_Always_Rejected(NegativeInt boardSize)
    {
        var settings = GameSettings.Default with { BoardSize = boardSize.Get };

        Assert.Contains(GameLogicErrors.InvalidBoardSize, settings.Validate());
    }

    [Property]
    public void Tengen_Exists_Only_On_Odd_Boards(PositiveInt boardSize)
    {
        var settings = GameSettings.Default with { BoardSize = boardSize.Get };

        if (boardSize.Get % 2 == 0)
        {
            Assert.Null(settings.Tengen);
        }
        else
        {
            Assert.Equal(new Point(boardSize.Get / 2, boardSize.Get / 2), settings.Tengen);
        }
    }

    [Property]
    public void Seven_By_Seven_Handicap_Never_Occupies_Tengen(PositiveInt value)
    {
        var handicap = new[] { 0, 2, 3, 4 }[value.Get % 4];

        var settings = GameSettings.Default with { BoardSize = 7, Handicap = handicap };

        var points = settings.HandicapPoints.ToArray();

        Assert.Equal(handicap, points.Length);
        Assert.Equal(points.Length, points.Distinct().Count());

        Assert.Equal(new Point(3, 3), settings.Tengen);
        Assert.DoesNotContain(new Point(3, 3), points);

        Assert.All(points, point =>
        {
            Assert.InRange(point.X, 0, 6);
            Assert.InRange(point.Y, 0, 6);
        });
    }

    [Property]
    public void EdgeDistance_Is_Always_Within_Board(PositiveInt boardSize)
    {
        var settings = GameSettings.Default with { BoardSize = boardSize.Get };

        if (settings.EdgeDistance is { } distance)
        {
            Assert.InRange(distance, 1, settings.BoardSize - 1);
        }
    }

    [Property]
    public void EdgeDistance_Never_Decreases_As_Board_Grows(PositiveInt first, PositiveInt second)
    {
        var smaller = GameSettings.Default with { BoardSize = Math.Min(first.Get, second.Get) };

        var larger = GameSettings.Default with { BoardSize = Math.Max(first.Get, second.Get) };

        if (
            smaller.EdgeDistance is { } smallerDistance
            && larger.EdgeDistance is { } largerDistance
        )
        {
            Assert.True(smallerDistance <= largerDistance);
        }
    }

    [Property]
    public void ValidHandicapRanges_Matches_Handicap_Rules(PositiveInt boardSize)
    {
        var settings = GameSettings.Default with { BoardSize = boardSize.Get };

        var expected = Enumerable
            .Range(0, 11)
            .Where(handicap =>
                settings.BoardSize switch
                {
                    < 7 => handicap == 0,
                    7 => handicap is 0 or (>= 2 and <= 4),
                    _ when settings.BoardSize % 2 == 0 => handicap is 0 or (>= 2 and <= 4),
                    _ => handicap is 0 or (>= 2 and <= 9),
                }
            );

        Assert.Equal(expected, settings.ValidHandicaps);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Handicap_Produces_Exactly_The_Requested_Number_Of_Unique_Points(
        HandicapScenario scenario
    )
    {
        var points = scenario.Settings.HandicapPoints.ToArray();

        Assert.Equal(scenario.Settings.Handicap, points.Length);
        Assert.Equal(points.Length, points.Distinct().Count());
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Handicap_Points_Are_Always_Within_Board(HandicapScenario scenario)
    {
        var points = scenario.Settings.HandicapPoints;

        foreach (var point in points)
        {
            Assert.InRange(point.X, 0, scenario.Settings.BoardSize - 1);
            Assert.InRange(point.Y, 0, scenario.Settings.BoardSize - 1);
        }
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Handicap_Corner_Points_Have_Correct_Edge_Distance(HandicapScenario scenario)
    {
        if (scenario.Settings.Handicap < 2)
            return;

        var settings = scenario.Settings;
        var distance = Assert.IsType<int>(settings.EdgeDistance);

        var near = distance - 1;
        var far = settings.BoardSize - distance;

        var expectedCorners = new HashSet<Point>
        {
            new(near, near),
            new(near, far),
            new(far, near),
            new(far, far),
        };

        var actual = settings.HandicapPoints.ToHashSet();

        // Handicap 4–9 always includes all four corner hoshi points.
        if (scenario.Settings.Handicap >= 4)
        {
            Assert.True(expectedCorners.IsSubsetOf(actual));
        }
    }

    [Property]
    public void Invalid_Board_Size_Suppresses_Handicap_Validation(
        NegativeInt boardSize,
        int handicap
    )
    {
        var settings = GameSettings.Default with { BoardSize = boardSize.Get, Handicap = handicap };

        var errors = settings.Validate().ToArray();

        Assert.Contains(GameLogicErrors.InvalidBoardSize, errors);
        Assert.DoesNotContain(GameLogicErrors.InvalidHandicap, errors);
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Valid_Game_Settings_Produce_No_Validation_Errors(GameSettings settings)
    {
        Assert.Empty(settings.Validate());
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Validation_Preserves_Time_Setting_Errors(GameSettings settings)
    {
        var expected = settings.TimeSettings.Validate().ToArray();
        var actual = settings.Validate().ToArray();

        foreach (var error in expected)
        {
            Assert.Contains(error, actual);
        }
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Board_Validation_Is_Independent_Of_Time_Settings(
        GameSettings first,
        GameSettings second
    )
    {
        var modified = first with { TimeSettings = second.TimeSettings };

        static Error[] BoardErrors(GameSettings settings) =>
            [
                .. settings
                    .Validate()
                    .Where(error =>
                        error == GameLogicErrors.InvalidBoardSize
                        || error == GameLogicErrors.InvalidHandicap
                    ),
            ];

        Assert.Equal(BoardErrors(first), BoardErrors(modified));
    }

    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Invalid_Handicap_Is_Always_Rejected(InvalidHandicapScenario scenario)
    {
        Assert.Contains(GameLogicErrors.InvalidHandicap, scenario.Settings.Validate());
    }
}
