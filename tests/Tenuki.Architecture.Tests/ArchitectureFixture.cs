using ArchUnitNET.Loader;

namespace Tenuki.Architecture.Tests;

public static class ArchitectureFixture
{
    public static readonly ArchUnitNET.Domain.Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            System.Reflection.Assembly.Load("Tenuki.Commons"),
            System.Reflection.Assembly.Load("Tenuki.Protocol"),
            System.Reflection.Assembly.Load("Tenuki.Server"),
            System.Reflection.Assembly.Load("Tenuki.Services.Application"),
            System.Reflection.Assembly.Load("Tenuki.Services.Logic"),
            System.Reflection.Assembly.Load("Tenuki.Sgf"),
            System.Reflection.Assembly.Load("Tenuki.Utils")
        )
        .Build();
}
