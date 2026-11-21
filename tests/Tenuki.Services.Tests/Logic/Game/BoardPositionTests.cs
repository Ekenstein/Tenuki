using FsCheck.Xunit;
using Tenuki.Services.Logic.Game;
using Tenuki.Services.Tests.Generators;
using Xunit;

namespace Tenuki.Services.Tests.Logic.Game;

public sealed class BoardPositionTests
{
    [Property(Arbitrary = [typeof(Arbitraries)])]
    public void Suicide_Move_Is_Rejected(SuicideScenario scenario)
    {
        var result = scenario.Position.PlaceStone(scenario.Stone, scenario.Settings);

        Assert.False(result.IsSuccess);
        Assert.Contains(GameLogicErrors.Suicide, result.Errors);
    }
}
