namespace Tenuki.Commons;

public enum PlayerColor
{
    Black,
    White,
}

public static class PlayerColorExtensions
{
    extension(PlayerColor color)
    {
        public PlayerColor Opponent =>
            color switch
            {
                PlayerColor.Black => PlayerColor.White,
                PlayerColor.White => PlayerColor.Black,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(color),
                    color,
                    "Unknown player color."
                ),
            };
    }
}
