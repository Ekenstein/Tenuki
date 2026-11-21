namespace Tenuki.Services.Logic.Game;

public static class GameLogicErrors
{
    public static readonly Error InvalidBoardSize = new(
        nameof(InvalidBoardSize),
        "The board size must be larger than or equal to 1"
    );
    public static readonly Error InvalidHandicap = new(
        nameof(InvalidHandicap),
        "The board size does not support the specified handicap"
    );
    public static readonly Error InvalidTimeSettings = new(
        nameof(InvalidTimeSettings),
        "The time settings are invalid"
    );
    public static readonly Error StoneOutOfBounds = new(
        nameof(StoneOutOfBounds),
        "The stone must not be placed outside of the board."
    );
    public static readonly Error PointOccupied = new(
        nameof(PointOccupied),
        "The stone can not be placed on this point because it is occupied by another stone."
    );
    public static readonly Error NotPlayerTurn = new(
        nameof(NotPlayerTurn),
        "It's not the player's turn."
    );
    public static readonly Error PositionIsRepeating = new(
        nameof(PositionIsRepeating),
        "The position is repeating (Ko)"
    );
    public static readonly Error GameAlreadyFinished = new(
        nameof(GameAlreadyFinished),
        "The game has already been finished."
    );
    public static readonly Error Suicide = new(nameof(Suicide), "Suicide moves are not permitted.");
    public static readonly Error NothingToUndo = new(
        nameof(NothingToUndo),
        "There's nothing to undo."
    );
    public static readonly Error NoUndoRequest = new(
        nameof(NoUndoRequest),
        "There's no request for undo."
    );
    public static readonly Error NotAuthorizedToAcceptUndo = new(
        nameof(NotAuthorizedToAcceptUndo),
        "Only the opponent can accept an undo."
    );
    public static readonly Error NotAuthorizedToRejectUndo = new(
        nameof(NotAuthorizedToRejectUndo),
        "Only the opponent can reject an undo."
    );

    public static readonly Error CountingNotInitiated = new(
        nameof(CountingNotInitiated),
        "Counting has not been initiated"
    );
}
