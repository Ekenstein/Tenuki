using System.Net;
using Tenuki.Commons;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Tests.Utils;
using Xunit;

namespace Tenuki.Server.Tests.Http.GameLobby;

public sealed class GetGameLobbyTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    [Fact]
    public async Task Get_Game_Lobby_Returns_404_NotFound_When_Lobby_Does_Not_Exist()
    {
        using var response = await fixture.GameLobbies.GetGameLobbyAsync(
            GameId.NewId(),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Game_Lobby_Returns_200_Ok_When_Lobby_Exist()
    {
        var gameLobby = await fixture
            .PostGameLobbyAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        using var response = await fixture.GameLobbies.GetGameLobbyAsync(
            gameLobby.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var expected = new GameLobbyState.Waiting(gameLobby.Id, GameLobbySettings.Default, [], 1);
        Assert.Equal(expected, await response.GetValueAsync(TestContext.Current.CancellationToken));
    }
}
