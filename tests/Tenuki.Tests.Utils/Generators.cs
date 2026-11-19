using FsCheck.Fluent;

namespace Tenuki.Tests.Utils;

public static class Generators
{
    public static readonly Gen<char> UppercaseChar = Gen.Choose('A', 'Z').Select(n => (char)n);
}
