using Tenuki.Server.Tests.Fixtures;
using Tenuki.Services.Logic.GameLobby;
using Tenuki.Tests.Utils;
using Xunit;

namespace Tenuki.Server.Tests.Games;

public sealed class GetLiveGamesTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    [Fact]
    public async Task Game_With_No_Subscribers_Are_Not_Included_In_Live_Games()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        var liveGames = await fixture.GetLiveGamesAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(game, liveGames.Games);
    }

    [Fact]
    public async Task Game_With_Subscribers_Is_Included_In_Live_Games()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        await using var client = fixture.CreateHubClient();
        await client.StartAsync(TestContext.Current.CancellationToken);
        await client.JoinGameAsync(game.Id, TestContext.Current.CancellationToken);
        var liveGames = await fixture.GetLiveGamesAsync(TestContext.Current.CancellationToken);
        Assert.Contains(game, liveGames.Games);
    }

    [Fact]
    public async Task Game_With_A_Disconnected_Subscriber_Is_Not_Included_In_Live_Games()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        await using (var client = fixture.CreateHubClient())
        {
            await client.StartAsync(TestContext.Current.CancellationToken);
            await client.JoinGameAsync(game.Id, TestContext.Current.CancellationToken);
        }

        var liveGames = await fixture.GetLiveGamesAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(game, liveGames.Games);
    }

    [Fact]
    public async Task Game_With_An_Expired_Subscriber_Is_Not_Included_In_Live_Games()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        await using var client = fixture.CreateHubClient();
        await client.StartAsync(TestContext.Current.CancellationToken);
        await client.JoinGameAsync(game.Id, TestContext.Current.CancellationToken);
        await fixture.ExpireConnectionPresenceAsync(
            client.ConnectionId,
            TestContext.Current.CancellationToken
        );
        var liveGames = await fixture.GetLiveGamesAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(game, liveGames.Games);
    }

    [Fact]
    public async Task Refreshing_Connection_Presence_Restores_Live_Game()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        await using var client = fixture.CreateHubClient();
        await client.StartAsync(TestContext.Current.CancellationToken);
        await client.JoinGameAsync(game.Id, TestContext.Current.CancellationToken);

        // Initially live.
        var liveGames = await fixture.GetLiveGamesAsync(TestContext.Current.CancellationToken);
        Assert.Contains(game, liveGames.Games);

        // Simulate expired presence.
        await fixture.ExpireConnectionPresenceAsync(
            client.ConnectionId,
            TestContext.Current.CancellationToken
        );

        liveGames = await fixture.GetLiveGamesAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(game, liveGames.Games);

        // Refresh all registered connections.
        await fixture.RefreshConnectionPresenceAsync(TestContext.Current.CancellationToken);

        liveGames = await fixture.GetLiveGamesAsync(TestContext.Current.CancellationToken);
        Assert.Contains(game, liveGames.Games);
    }
}
