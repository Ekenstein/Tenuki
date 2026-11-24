using FsCheck.Xunit;

namespace Tenuki.Utils.Tests;

public sealed class IdTests
{
    [Property]
    public bool Id_Can_Roundtrip_Through_String(Guid value)
    {
        var id = new TestId(value);

        return TestId.TryParse(id.ToString(), null, out var parsed)
            && parsed == id;
    }

    private sealed record TestId(Guid Value) : Id<TestId>(Value);
}
