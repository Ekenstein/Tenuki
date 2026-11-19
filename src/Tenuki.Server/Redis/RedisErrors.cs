namespace Tenuki.Server.Redis;

public static class RedisErrors
{
    public static readonly Error NoMessageEntry = new(
        nameof(NoMessageEntry),
        "There was no message entry in the notification."
    );
    public static readonly Error InvalidMessageFormat = new(
        nameof(InvalidMessageFormat),
        "The message was in an invalid format and could not be deserialized."
    );
    internal static readonly Error GameIdNotFound = new(
        nameof(GameIdNotFound),
        "The entries does not contain the game id."
    );
    internal static readonly Error InvalidGameId = new(
        nameof(InvalidGameId),
        "The game id is invalid."
    );
    internal static readonly Error RevisionNotFound = new(
        nameof(RevisionNotFound),
        "The entries does not contain the revision."
    );
    internal static readonly Error InvalidRevision = new(
        nameof(InvalidRevision),
        "The revision is invalid."
    );
    internal static readonly Error StateTokenNotFound = new(
        nameof(StateTokenNotFound),
        "The entries does not contain the state token."
    );
    internal static readonly Error CommittedCommandsNotFound = new(
        nameof(CommittedCommandsNotFound),
        "The entries does not contain committed commands"
    );
    internal static readonly Error InvalidCommittedCommands = new(
        nameof(InvalidCommittedCommands),
        "The committed commands are invalid."
    );
}
