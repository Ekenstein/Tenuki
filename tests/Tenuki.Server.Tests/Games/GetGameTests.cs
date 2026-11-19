using System.Net;
using Tenuki.Commons;
using Tenuki.Server.Models;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Tests.Utils;
using Xunit;

namespace Tenuki.Server.Tests.Games;

public sealed class GetGameTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    [Fact]
    public async Task Get_Game_Returns_NotFound_If_Game_Does_Not_Exist()
    {
        using var response = await fixture.Games.GetGameAsync(
            GameId.NewId(),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Game_Returns_Ok_With_Game_Description()
    {
        var game = await fixture
            .PostGameAsync(PostGameRequest.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        using var response = await fixture.Games.GetGameAsync(
            game.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var actualGame = await response.GetValueAsync(TestContext.Current.CancellationToken);
        Assert.Equal(game, actualGame);
    }
}
