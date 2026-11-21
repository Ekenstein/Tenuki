using Tenuki.Commons;

namespace Tenuki.Services.Logic.Game;

internal static class GameStateExtensions
{
    extension(GameState state)
    {
        internal PlayerColor NextPlayer =>
            state.History.LastOrDefault() switch
            {
                BoardEvent evt => evt.Color.Opponent,
                null => state.Settings.Handicap > 0 ? PlayerColor.White : PlayerColor.Black,
            };

        internal bool IsTimeOver =>
            state.NextPlayer switch
            {
                PlayerColor.Black => state.Clock.Black.IsTimeOver,
                PlayerColor.White => state.Clock.White.IsTimeOver,
                _ => throw new NotSupportedException(
                    $"The color '{state.NextPlayer}' is not supported."
                ),
            };

        internal BoardPosition? GetPreviousPosition()
        {
            var historyWithoutLastMove = state.History.InitOrNull();
            if (historyWithoutLastMove == null)
            {
                return null;
            }

            return historyWithoutLastMove.Replay(state.Settings).ValueOrThrow(CorruptHistory);

            static InvalidOperationException CorruptHistory(IEnumerable<Error> errors)
            {
                var errorCodes = string.Join(", ", errors.Select(x => x.Code));
                return new(
                    $"The game state contains a corrupt history. The errors were '{errorCodes}'"
                );
            }
        }

        internal GameState IncreaseRevision() => state with { Revision = state.Revision + 1 };

        internal GameState UpdateClock(DateTimeOffset now) =>
            state with
            {
                Clock = state.Clock.UpdateClockState(
                    now,
                    state.NextPlayer,
                    state.Settings.TimeSettings
                ),
            };

        internal GameState InitiateCounting() => state with { Counting = new([], []) };

        internal GameState CompleteTurn() =>
            state with
            {
                Clock = state.Clock.CompleteTurn(state.NextPlayer, state.Settings.TimeSettings),
            };
    }
}
