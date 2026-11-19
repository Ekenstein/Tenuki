using Tenuki.Server.Options;
using Tenuki.Server.Redis;
using Tenuki.Server.Tests.Fixtures;
using Xunit;

namespace Tenuki.Server.Tests.Redis;

public sealed class RedisConnectionPresenceTests(RedisFixture fixture) : IClassFixture<RedisFixture>
{
    private readonly RedisConnectionPresence Instance = CreateInstance(fixture, new());

    [Fact]
    public async Task Is_Active_Returns_False_For_A_Non_Existing_Connection_Id()
    {
        var result = await Instance.IsActiveAsync(
            ConnectionId(),
            TestContext.Current.CancellationToken
        );
        Assert.False(result);
    }

    [Fact]
    public async Task Is_Active_Returns_False_For_An_Empty_Connection_Id()
    {
        var result = await Instance.IsActiveAsync("", TestContext.Current.CancellationToken);
        Assert.False(result);
    }

    [Fact]
    public async Task Is_Active_Returns_False_For_An_Unregistered_Connection_Id()
    {
        var connectionId = ConnectionId();
        await Instance.RegisterAsync(connectionId, TestContext.Current.CancellationToken);
        Assert.True(
            await Instance.IsActiveAsync(connectionId, TestContext.Current.CancellationToken)
        );

        Assert.True(
            await Instance.UnregisterAsync(connectionId, TestContext.Current.CancellationToken)
        );
        Assert.False(
            await Instance.IsActiveAsync(connectionId, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task Is_Active_Returns_True_For_A_Refreshed_Connection_Id()
    {
        const long ttlInMs = 50L;

        var optionsWithShortTtl = new ConnectionPresenceOptions
        {
            Ttl = new() { LifetimeInMs = ttlInMs },
        };

        var instance = CreateInstance(fixture, optionsWithShortTtl);

        var connectionId = ConnectionId();
        await instance.RegisterAsync(connectionId, TestContext.Current.CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(ttlInMs), TestContext.Current.CancellationToken);
        Assert.False(
            await instance.IsActiveAsync(connectionId, TestContext.Current.CancellationToken)
        );

        await instance.RefreshAsync(connectionId, TestContext.Current.CancellationToken);
        Assert.True(
            await instance.IsActiveAsync(connectionId, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task An_Unregistered_Connection_Id_That_Is_Refreshed_Is_Active()
    {
        var connectionId = ConnectionId();
        await Instance.RegisterAsync(connectionId, TestContext.Current.CancellationToken);
        Assert.True(
            await Instance.UnregisterAsync(connectionId, TestContext.Current.CancellationToken)
        );
        Assert.False(
            await Instance.IsActiveAsync(connectionId, TestContext.Current.CancellationToken)
        );
        await Instance.RefreshAsync(connectionId, TestContext.Current.CancellationToken);
        Assert.True(
            await Instance.IsActiveAsync(connectionId, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task Unregister_A_Non_Existing_Connection_Id_Returns_False()
    {
        Assert.False(
            await Instance.UnregisterAsync(ConnectionId(), TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task Is_Active_Returns_False_For_A_Connection_That_Has_Expired()
    {
        const long ttlInMs = 50L;

        var optionsWithShortTtl = new ConnectionPresenceOptions
        {
            Ttl = new() { LifetimeInMs = ttlInMs },
        };

        var instance = CreateInstance(fixture, optionsWithShortTtl);

        var connectionId = ConnectionId();
        await instance.RegisterAsync(connectionId, TestContext.Current.CancellationToken);
        Assert.True(
            await instance.IsActiveAsync(connectionId, TestContext.Current.CancellationToken)
        );

        await Task.Delay(TimeSpan.FromMilliseconds(ttlInMs), TestContext.Current.CancellationToken);

        Assert.False(
            await instance.IsActiveAsync(connectionId, TestContext.Current.CancellationToken)
        );
    }

    private static string ConnectionId() => Guid.NewGuid().ToString();

    private static RedisConnectionPresence CreateInstance(
        RedisFixture fixture,
        ConnectionPresenceOptions options
    )
    {
        return new(fixture.Connection, Microsoft.Extensions.Options.Options.Create(options));
    }
}
