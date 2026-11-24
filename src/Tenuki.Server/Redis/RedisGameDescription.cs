using System.Globalization;
using StackExchange.Redis;
using Tenuki.Commons;

namespace Tenuki.Server.Redis;

internal static class RedisGameDescription
{
    internal static readonly RedisValue GameIdEntryName = "gameId";
    internal static readonly RedisValue RevisionEntryName = "revision";
    internal static readonly RedisValue StateTokenEntryName = "stateToken";

    internal static HashEntry[] ToEntries(this GameDescription game) =>
        [
            new(GameIdEntryName, game.Id.ToString()),
            new(RevisionEntryName, game.Revision.ToString(CultureInfo.InvariantCulture)),
            new(StateTokenEntryName, game.StateToken),
        ];

    internal static Result<GameDescription> ToGameDescription(this HashEntry[] entries)
    {
        var valuesByName = entries.ToDictionary(x => x.Name, x => x.Value);
        if (!valuesByName.TryGetValue(RevisionEntryName, out var revisionValue))
        {
            return new Failure([RedisErrors.RevisionNotFound]);
        }

        if (
            !long.TryParse(revisionValue.ToString(), CultureInfo.InvariantCulture, out var revision)
        )
        {
            return new Failure([RedisErrors.InvalidRevision]);
        }

        if (!valuesByName.TryGetValue(StateTokenEntryName, out var stateToken))
        {
            return new Failure([RedisErrors.StateTokenNotFound]);
        }

        if (!valuesByName.TryGetValue(GameIdEntryName, out var sGameId))
        {
            return new Failure([RedisErrors.GameIdNotFound]);
        }

        if (!GameId.TryParse(sGameId.ToString(), CultureInfo.InvariantCulture, out var gameId))
        {
            return new Failure([RedisErrors.InvalidGameId]);
        }

        return Result.Success(new GameDescription(gameId, stateToken.ToString(), revision));
    }
}
