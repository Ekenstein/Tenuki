namespace Tenuki.Protocol;

public sealed record CommandId(Guid Value) : Id<CommandId>(Value)
{
    public CommandId()
        : this(Guid.NewGuid()) { }
}
