using System.Net;
using Tenuki.Commons;
using Tenuki.Server.Tests.Fixtures;
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
}
