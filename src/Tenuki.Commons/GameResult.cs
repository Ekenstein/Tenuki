using System.Text.Json.Serialization;

namespace Tenuki.Commons;

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record GameResult(PlayerColor Winner)
{
    public sealed record Time(PlayerColor Winner) : GameResult(Winner);
    public sealed record Resign(PlayerColor Winner) : GameResult(Winner);
    public sealed record Score(double Points, PlayerColor Winner) : GameResult(Winner);
}
