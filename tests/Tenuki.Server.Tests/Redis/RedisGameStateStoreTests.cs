using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Tenuki.Commons;
using Tenuki.Server.Redis;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Services.Application.Game;
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
        var result = await Instance.GetAsync(GameId.NewId(), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_Returns_State_Token_For_Created_Game()
    {
        var expectedGame = new GameDescription(GameId.NewId(), "Hello World", 1);
        await SeedGameAsync(expectedGame);
        var actualGame = await Instance.GetAsync(
            expectedGame.Id,
            TestContext.Current.CancellationToken
        );
        Assert.NotNull(actualGame);
        Assert.Equal(expectedGame, actualGame);
    }

    [Fact]
    public async Task UpdateAsync_Returns_NotFound_If_Game_Does_Not_Exist()
    {
        var result = await Instance.UpdateAsync(
            new(GameId.NewId(), "", 0),
            1,
            [],
            TestContext.Current.CancellationToken
        );
        Assert.IsType<GameStateUpdateOutcome.NotFound>(result);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Updated_If_Game_Has_Expected_Revision()
    {
        var gameId = GameId.NewId();
        await SeedGameAsync(new(gameId, "Hello World", 0));
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
        var gameId = GameId.NewId();
        const string stateToken = "Hello World";
        await SeedGameAsync(new(gameId, stateToken, 0));
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

    private async Task SeedGameAsync(GameDescription game)
    {
        var db = fixture.Connection.GetDatabase();

        await db.HashSetAsync(RedisConstants.GameKey(game.Id), game.ToEntries())
            .WaitAsync(TestContext.Current.CancellationToken);
    }
}
