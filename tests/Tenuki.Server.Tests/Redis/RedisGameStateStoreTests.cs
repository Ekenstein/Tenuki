using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Tenuki.Commons;
using Tenuki.Server.Redis;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Services.Application;
using Xunit;

namespace Tenuki.Server.Tests.Redis;

public sealed class RedisGameStateStoreTests(RedisFixture fixture) : IClassFixture<RedisFixture>
{
    private static readonly IOptions<JsonOptions> JsonOptions =
        Microsoft.Extensions.Options.Options.Create(
            new JsonOptions().Apply(x => JsonUtils.ConfigureOptions(x.SerializerOptions))
        );
    private readonly RedisGameStateStore Instance = new(
        fixture.Connection,
        RedisStreamSettings.Default,
        JsonOptions
    );

    [Fact]
    public async Task GetAsync_Returns_Null_If_Game_Does_Not_Exist()
    {
        var result = await Instance.GetAsync(new(), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_Returns_State_Token_For_Created_Game()
    {
        var expectedGame = new GameDescription(new(), "Hello World", 1);

        Assert.True(
            await Instance.CreateAsync(expectedGame, TestContext.Current.CancellationToken)
        );
        var actualGame = await Instance.GetAsync(
            expectedGame.Id,
            TestContext.Current.CancellationToken
        );
        Assert.NotNull(actualGame);
        Assert.Equal(expectedGame, actualGame);
    }

    [Fact]
    public async Task CreateAsync_Returns_False_If_Game_With_Id_Already_Exist()
    {
        var gameId = new GameId();

        Assert.True(
            await Instance.CreateAsync(
                new(gameId, "Hello World", 1),
                TestContext.Current.CancellationToken
            )
        );
        Assert.False(
            await Instance.CreateAsync(
                new(gameId, "Another world", 2),
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task UpdateAsync_Returns_NotFound_If_Game_Does_Not_Exist()
    {
        var result = await Instance.UpdateAsync(
            new(new(), "", 0),
            1,
            [],
            TestContext.Current.CancellationToken
        );
        Assert.IsType<GameStateUpdateOutcome.NotFound>(result);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Updated_If_Game_Has_Expected_Revision()
    {
        var gameId = new GameId();
        Assert.True(
            await Instance.CreateAsync(
                new(gameId, "Hello World", 0),
                TestContext.Current.CancellationToken
            )
        );
        Assert.IsType<GameStateUpdateOutcome.Updated>(
            await Instance.UpdateAsync(
                new(gameId, "Another world", 1),
                0,
                [],
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task UpdateAsync_Returns_Conflict_If_Game_Does_Not_Have_Expected_Revision()
    {
        var gameId = new GameId();
        const string stateToken = "Hello World";
        Assert.True(
            await Instance.CreateAsync(
                new(gameId, stateToken, 0),
                TestContext.Current.CancellationToken
            )
        );
        var conflict = Assert.IsType<GameStateUpdateOutcome.Conflict>(
            await Instance.UpdateAsync(
                new(gameId, "Another world", 2),
                1,
                [],
                TestContext.Current.CancellationToken
            )
        );
        Assert.Equal(stateToken, conflict.CurrentStateToken);
    }
}
