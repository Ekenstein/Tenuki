using System.Text.Json.Serialization;

namespace Tenuki.Commons;

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record BoardEvent(PlayerColor Color)
{
    public sealed record Stone(PlayerColor Color, Point Point) : BoardEvent(Color);
    public sealed record Pass(PlayerColor Color) : BoardEvent(Color);
}
