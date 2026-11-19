using Tenuki.Commons;

namespace Tenuki.Services.Logic;

internal static class BoardEventExtensions
{
    internal static Result<BoardPosition> Replay(
        this IEnumerable<BoardEvent> events,
        GameSettings settings
    ) =>
        events.Aggregate(
            Result.Success(settings.InitialBoardPosition),
            (boardPosition, evt) =>
                evt switch
                {
                    BoardEvent.Pass => boardPosition,
                    BoardEvent.Stone stone => boardPosition.SelectMany(position =>
                        position.PlaceStone(new(stone.Color, stone.Point), settings)
                    ),
                }
        );
}
