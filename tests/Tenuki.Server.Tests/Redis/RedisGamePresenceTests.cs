using Moq;
using Tenuki.Commons;
using Tenuki.Server.Redis;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Services.Application;
using Xunit;

namespace Tenuki.Server.Tests.Redis;

public sealed class RedisGamePresenceTests(RedisFixture fixture) : IClassFixture<RedisFixture>
{
    [Fact]
    public async Task A_Game_With_No_Active_Connections_Is_Not_Included_In_Live_Games()
    {
        var connectionId = ConnectionId();
        var gameId = new GameId();

        var connectionPresence = new Mock<IConnectionPresence>()
            .Apply(x =>
            {
                x.Setup(x => x.IsActiveAsync(connectionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
            })
            .Object;

        var instance = CreateInstance(fixture, connectionPresence);

        await instance.JoinGameAsync(gameId, connectionId, TestContext.Current.CancellationToken);
        var liveGames = await instance
            .GetLiveGamesAsync(TestContext.Current.CancellationToken)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain(gameId, liveGames);
    }

    [Fact]
    public async Task A_Game_At_Least_One_Active_Connection_Is_Included_In_Live_Games()
    {
        var connectionId = ConnectionId();
        var gameId = new GameId();

        var connectionPresence = new Mock<IConnectionPresence>()
            .Apply(x =>
            {
                x.Setup(x => x.IsActiveAsync(connectionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
            })
            .Object;

        var instance = CreateInstance(fixture, connectionPresence);

        await instance.JoinGameAsync(gameId, connectionId, TestContext.Current.CancellationToken);

        Assert.Contains(gameId, instance.GetLiveGamesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_Game_Where_All_Connections_Has_Left_The_Game_Is_Not_Included_In_Live_Games()
    {
        var connectionId = ConnectionId();
        var gameId = new GameId();

        var connectionPresence = new Mock<IConnectionPresence>()
            .Apply(x =>
            {
                x.Setup(x => x.IsActiveAsync(connectionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
            })
            .Object;

        var instance = CreateInstance(fixture, connectionPresence);
        await instance.JoinGameAsync(gameId, connectionId, TestContext.Current.CancellationToken);

        Assert.Contains(gameId, instance.GetLiveGamesAsync(TestContext.Current.CancellationToken));

        await instance.LeaveGameAsync(gameId, connectionId, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(
            gameId,
            instance.GetLiveGamesAsync(TestContext.Current.CancellationToken)
        );
    }

    private static RedisGamePresence CreateInstance(
        RedisFixture fixture,
        IConnectionPresence connectionPresence
    )
    {
        return new(fixture.Connection, connectionPresence);
    }

    private static string ConnectionId() => Guid.NewGuid().ToString();
}
