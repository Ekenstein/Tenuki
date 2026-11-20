using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Tenuki.Server.Options;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Server.Tests.Utils;
using Tenuki.Services.Application;
using Xunit;

namespace Tenuki.Server.Tests;

public sealed class BootstrapTests
{
    [Fact]
    public void Start_Server_Without_Signature_Config_Throws()
    {
        var config = ServerFactory.ValidConfig.Remove("HmacToken:Secret");
        using var server = new ServerFactory(new FakeTimeProvider(), config);
        var exception = Assert.Throws<OptionsValidationException>(server.CreateClient);
    }

    [Fact]
    public void Start_Server_Without_Redis_Config_Throws()
    {
        var config = ServerFactory.ValidConfig.Remove("Redis:ConnectionString");
        using var server = new ServerFactory(new FakeTimeProvider(), config);
        var exception = Assert.Throws<OptionsValidationException>(server.CreateClient);
    }

    [Fact]
    public void Can_Configure_Game_Notifications()
    {
        var options = new GameNotificationOptions
        {
            BackgroundService = new() { DelayInMs = 1337 },
        };
        AssertCanConfigure(options, "GameNotifications");
    }

    [Fact]
    public void Can_Configure_Game_Notification_Recovery()
    {
        var options = new GameNotificationRecoveryOptions
        {
            BackgroundService = new() { DelayInMs = 500 },
            BatchSize = 10,
            MaxNumberOfIterations = 100,
            MinIdleTimeInMs = 10,
        };

        AssertCanConfigure(options, "GameNotificationRecovery");
    }

    [Fact]
    public void Can_Configure_GamePresence()
    {
        var options = new GamePresenceOptions { BackgroundService = new() { DelayInMs = 1337 } };
        AssertCanConfigure(options, "GamePresence");
    }

    [Fact]
    public void Can_Configure_HmacToken()
    {
        var options = new HmacTokenOptions { Secret = "Hello World" };
        AssertCanConfigure(options, "HmacToken");
    }

    [Fact]
    public void Can_Configure_ConnectionPresence()
    {
        var options = new ConnectionPresenceOptions
        {
            BackgroundService = new() { DelayInMs = 1337 },
            Ttl = new() { LifetimeInMs = 1337 },
        };

        AssertCanConfigure(options, "ConnectionPresence");
    }

    private static void AssertCanConfigure<T>(T options, string sectionName)
        where T : class
    {
        var config = ServerFactory.ValidConfig.SetItems(options.ToConfiguration(sectionName));

        using var server = new ServerFactory(new FakeTimeProvider(), config);
        var actual = server.Services.GetRequiredService<IOptions<T>>();
        Assert.Equivalent(options, actual.Value, strict: true);
    }

    [Fact]
    public void Start_Server_With_Valid_Config_Does_Not_Throw()
    {
        using var server = new ServerFactory(new FakeTimeProvider(), ServerFactory.ValidConfig);
        using var client = server.CreateClient();
        Assert.True(true, "Succesfully started the server.");
    }
}
