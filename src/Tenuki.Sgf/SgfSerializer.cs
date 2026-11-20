using System.Text;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Tenuki.Sgf;

public static class SgfSerializer
{
    private const char EscapeChar = '\\';
    private const char ColonChar = ':';
    private const char RightBracketChar = ']';
    private const char LeftBracketChar = '[';
    private const char SemicolonChar = ';';
    private const char LeftParanthesisChar = '(';
    private const char RightParanthesisChar = ')';

    private static readonly Parser<char, string> PropertyIdentifier = Uppercase.AtLeastOnceString();

    //private static readonly Parser<char, string> NormalChar = AnyCharExcept(RightBracketChar, EscapeChar).Select(c => c.ToString());
    //private static readonly Parser<char, string> EscapedChar = Char(EscapeChar).Then(OneOf(ColonChar, EscapeChar, RightBracketChar)).Select(c => c.ToString());
    private static readonly Parser<char, string> EscapedChar = Char(EscapeChar)
        .Then(Any)
        .Select(c => c.ToString());

    private static readonly Parser<char, string> NormalChar = AnyCharExcept(
            RightBracketChar,
            EscapeChar
        )
        .Select(c => c.ToString());
    private static readonly Parser<char, string> PropertyValue = OneOf(EscapedChar, NormalChar)
        .ManyString()
        .Between(Char(LeftBracketChar), Char(RightBracketChar));

    private static readonly Parser<char, SgfProperty> Property =
        from propertyIdentifier in PropertyIdentifier
        from values in PropertyValue.AtLeastOnce()
        select new SgfProperty(propertyIdentifier, [.. values]);

    private static readonly Parser<char, SgfNode> Node = Char(SemicolonChar)
        .Then(Property.Many())
        .Select(properties => new SgfNode([.. properties]));

    private static Parser<char, SgfGameTree> Tree() =>
        from _ in Char(LeftParanthesisChar)
        from sequence in Node.AtLeastOnce()
        from trees in Tree().Many()
        from __ in Char(RightParanthesisChar)
        select new SgfGameTree([.. sequence], [.. trees]);

    public static SgfGameTree Deserialize(string sgf) => Tree().ParseOrThrow(sgf);

    public static string Serialize(SgfGameTree tree)
    {
        var sb = new StringBuilder();
        SerializeGameTree(tree, sb);
        return sb.ToString();
    }

    private static void SerializeGameTree(SgfGameTree tree, StringBuilder stringBuilder)
    {
        stringBuilder.Append(LeftParanthesisChar);
        foreach (var node in tree.Sequence)
        {
            SerializeNode(node, stringBuilder);
        }
        foreach (var subtree in tree.Trees)
        {
            SerializeGameTree(subtree, stringBuilder);
        }
        stringBuilder.Append(RightParanthesisChar);
    }

    private static void SerializeNode(SgfNode node, StringBuilder stringBuilder)
    {
        stringBuilder.Append(SemicolonChar);
        foreach (var property in node.Properties)
        {
            stringBuilder.Append(property.Identifier);
            if (property.Values.IsEmpty)
            {
                stringBuilder.Append("[]");
            }
            else
            {
                foreach (var value in property.Values)
                {
                    stringBuilder.Append(LeftBracketChar);
                    AppendEscapedValue(stringBuilder, value);
                    stringBuilder.Append(RightBracketChar);
                }
            }
        }
    }

    private static void AppendEscapedValue(StringBuilder sb, string value)
    {
        foreach (var c in value)
        {
            if (c is '\\' or ']')
            {
                sb.Append('\\');
            }

            sb.Append(c);
        }
    }
}
