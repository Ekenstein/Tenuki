using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Server.BackgroundServices;
using Tenuki.Server.Tests.Clients;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Tests.Utils;
using Xunit;

namespace Tenuki.Server.Tests.BackgroundServices;

public sealed class GameNotificationBackgroundServiceTests
{
    [Fact]
    public async Task Queued_Game_Notification_Is_Dispatched_After_Configured_Delay()
    {
        await using var fixture = new ServerFixture();
        await using var client = await StartHubConnectionAndJoinLobbyAsync(fixture);

        await fixture.WaitForBackgroundServiceToStartAsync<GameNotificationBackgroundService>(
            TestContext.Current.CancellationToken
        );

        var game = await CreateGameAsync(fixture);

        await AssertDispatchedGamesAsync(fixture, client, 1, game);
    }

    [Fact]
    public async Task Queued_Game_Notifications_Are_Dispatched_On_Subsequent_Ticks()
    {
        await using var fixture = new ServerFixture();
        await using var client = await StartHubConnectionAndJoinLobbyAsync(fixture);

        await fixture.WaitForBackgroundServiceToStartAsync<GameNotificationBackgroundService>(
            TestContext.Current.CancellationToken
        );

        var gameA = await CreateGameAsync(fixture);

        await AssertDispatchedGamesAsync(fixture, client, 1, gameA);

        var gameB = await CreateGameAsync(fixture);

        await AssertDispatchedGamesAsync(fixture, client, 2, gameB);
    }

    [Fact]
    public async Task Multiple_Queued_Game_Notifications_Are_Dispatched_On_Next_Tick()
    {
        await using var fixture = new ServerFixture();
        await using var client = await StartHubConnectionAndJoinLobbyAsync(fixture);

        await fixture.WaitForBackgroundServiceToStartAsync<GameNotificationBackgroundService>(
            TestContext.Current.CancellationToken
        );

        var gameA = await CreateGameAsync(fixture);
        var gameB = await CreateGameAsync(fixture);
        await AssertDispatchedGamesAsync(fixture, client, 1, gameA, gameB);
    }

    [Fact]
    public async Task Queued_Game_Notification_Is_Dispatched_After_Empty_Tick()
    {
        await using var fixture = new ServerFixture();
        await using var client = await StartHubConnectionAndJoinLobbyAsync(fixture);

        await fixture.WaitForBackgroundServiceToStartAsync<GameNotificationBackgroundService>(
            TestContext.Current.CancellationToken
        );

        // No messages queued.
        await fixture.AdvanceBackgroundServiceAsync<GameNotificationBackgroundService>(
            1,
            TestContext.Current.CancellationToken
        );

        Assert.False(client.LobbyMessages.TryRead(out _));

        // Queue a message after the empty tick.
        var game = await CreateGameAsync(fixture);

        await AssertDispatchedGamesAsync(fixture, client, 2, game);
    }

    private static async Task AssertDispatchedGamesAsync(
        ServerFixture fixture,
        TenukiClient client,
        long tick,
        params GameDescription[] games
    )
    {
        await fixture.AdvanceBackgroundServiceAsync<GameNotificationBackgroundService>(
            tick,
            TestContext.Current.CancellationToken
        );

        var remaining = games.ToHashSet();

        var received = await client.LobbyMessages.WaitForMessageAsync<LobbyMessage.GamesAdded>(
            message =>
            {
                remaining.ExceptWith(message.Games);
                return remaining.Count == 0;
            },
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(received);
        Assert.Empty(remaining);
    }

    private static Task<GameDescription> CreateGameAsync(ServerFixture fixture) =>
        fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

    private static async Task<TenukiClient> StartHubConnectionAndJoinLobbyAsync(
        ServerFixture fixture
    )
    {
        var client = fixture.CreateHubClient();
        try
        {
            await client.StartAsync(TestContext.Current.CancellationToken);
            await client.JoinLobbyAsync(TestContext.Current.CancellationToken);
            return client;
        }
        catch
        {
            await client.DisposeAsync();
            throw;
        }
    }
}
