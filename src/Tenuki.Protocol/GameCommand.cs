using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Tenuki.Commons;

namespace Tenuki.Protocol;

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record GameCommand(CommandId CommandId, PlayerColor Color)
{
    public sealed record Pass(CommandId CommandId, PlayerColor Color)
        : GameCommand(CommandId, Color);

    public sealed record Stone(CommandId CommandId, PlayerColor Color, Point Point)
        : GameCommand(CommandId, Color);

    public sealed record Resign(CommandId CommandId, PlayerColor Color)
        : GameCommand(CommandId, Color);

    public sealed record RequestUndo(CommandId CommandId, PlayerColor Color)
        : GameCommand(CommandId, Color);

    public sealed record AcceptUndo(CommandId CommandId, PlayerColor Color)
        : GameCommand(CommandId, Color);

    public sealed record RejectUndo(CommandId CommandId, PlayerColor Color)
        : GameCommand(CommandId, Color);

    public sealed record MarkDeadStones(
        CommandId CommandId,
        PlayerColor Color,
        ImmutableHashSet<Point> Points
    ) : GameCommand(CommandId, Color);

    public sealed record UnmarkDeadStones(
        CommandId CommandId,
        PlayerColor Color,
        ImmutableHashSet<Point> Points
    ) : GameCommand(CommandId, Color);

    public sealed record AcceptScore(CommandId CommandId, PlayerColor Color)
        : GameCommand(CommandId, Color);

    public sealed record ResumePlay(CommandId CommandId, PlayerColor Color)
        : GameCommand(CommandId, Color);
}
