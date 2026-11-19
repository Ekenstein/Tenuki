using FsCheck.Xunit;
using System.Text.Json;
using Xunit;

namespace Tenuki.Sgf.Tests;

public sealed class SgfSerializerTests
{
    [Theory]
    [InlineData("Hello]world")]
    [InlineData("Hello\\world")]
    [InlineData("Hello:world")]
    [InlineData("Hello\tworld")]
    [InlineData("Hello\r\nworld")]
    public void PropertyValue_RoundTrip(string value)
    {
        var tree = new SgfGameTree([new SgfNode([new SgfProperty("C", [value])])], []);

        var sgf = SgfSerializer.Serialize(tree);
        var actual = SgfSerializer.Deserialize(sgf);

        Assert.Equivalent(tree, actual);
    }

    [Property(Arbitrary = [typeof(SgfArbitraries)])]
    public void Serialize_Then_Deserialize_Gives_Same_Tree(GeneratedTree generatedTree)
    {
        var tree = generatedTree.ToSgfGameTree();

        var sgf = SgfSerializer.Serialize(tree);
        var actual = SgfSerializer.Deserialize(sgf);

        Assert.Equal(
            JsonSerializer.Serialize(tree, JsonUtils.PrettyDefaultOptions),
            JsonSerializer.Serialize(actual, JsonUtils.PrettyDefaultOptions)
        );
    }
}
