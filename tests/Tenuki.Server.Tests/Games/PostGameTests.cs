using Tenuki.Protocol;
using Tenuki.Server.Models;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Tests.Utils;
using Xunit;

namespace Tenuki.Server.Tests.Games;

public sealed class PostGameTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    [Fact]
    public async Task Post_Game_With_Valid_Settings_Broadcasts_Game_In_Lobby()
    {
        await using var client = fixture.CreateHubClient();
        await client.StartAsync(TestContext.Current.CancellationToken);
        await client.JoinLobbyAsync(TestContext.Current.CancellationToken);

        var game = await fixture
            .PostGameAsync(PostGameRequest.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        await fixture.ProcessIncomingMessagesAsync(TestContext.Current.CancellationToken);

        var lobbyMessage = await client.LobbyMessages.WaitForMessageAsync<LobbyMessage.GamesAdded>(
            message => message.Games.Contains(game),
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(lobbyMessage);
    }
}
