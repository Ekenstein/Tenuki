namespace Tenuki.Commons;

public sealed record GameId(Guid Value) : Id<GameId>(Value)
{
    public GameId()
        : this(Guid.NewGuid()) { }
};
