using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Tenuki.Server.Tests.Fixtures;
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
    public void Start_Server_With_Valid_Config_Does_Not_Throw()
    {
        using var server = new ServerFactory(new FakeTimeProvider(), ServerFactory.ValidConfig);
        using var client = server.CreateClient();
        Assert.True(true, "Succesfully started the server.");
    }
}
