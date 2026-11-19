using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.xUnitV3;
using Tenuki.Protocol;
using Tenuki.Server.SignalR;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Tenuki.Architecture.Tests;

public sealed partial class ArchitectureTests
{
    private const string FunctionalCore = "Tenuki.Services.Logic";
    private const string ImperativeShell = "Tenuki.Services.Application";

    [Fact]
    public void No_Cycles_Allowed() =>
        SliceRuleDefinition
            .Slices()
            .Matching("(**)")
            .Should()
            .BeFreeOfCycles()
            .Check(ArchitectureFixture.Architecture);

    [Fact]
    public void Async_Functions_Must_Be_Suffixed_With_Async()
    {
        MethodMembers()
            .That()
            .HaveReturnType(typeof(Task), typeof(Task<>), typeof(ValueTask), typeof(ValueTask<>))
            .And()
            .AreNotDeclaredIn(typeof(ITenukiClient), typeof(TenukiHub)) // exempted types
            .Should()
            .HaveNameMatching(@"^[^(]+Async\(.*\)$")
            .Check(ArchitectureFixture.Architecture);
    }

    [Fact]
    public void SignalR_Api_Must_Not_Have_Functions_Suffixed_With_Async()
    {
        var exemptedDueToNotBeingPartOfApi = MethodMembers()
            .That()
            .AreDeclaredIn(typeof(TenukiHub))
            .And()
            .HaveNameContaining(nameof(TenukiHub.OnConnectedAsync))
            .Or()
            .HaveNameContaining(nameof(TenukiHub.OnDisconnectedAsync));

        MethodMembers()
            .That()
            .AreNot(exemptedDueToNotBeingPartOfApi)
            .And()
            .AreDeclaredIn(typeof(ITenukiClient), typeof(TenukiHub))
            .Should()
            .NotHaveNameMatching(@"^[^(]+Async\(.*\)$")
            .Check(ArchitectureFixture.Architecture);
    }
}
