namespace Tenuki.Server.Messaging;

public static class MessageEnvelope
{
    public static MessageEnvelope<TId, TMessage> Create<TId, TMessage>(TId id, TMessage message) =>
        new(id, message);
}

public sealed record MessageEnvelope<TId, TMessage>(TId Id, TMessage Message)
{
    public MessageEnvelope<TId, U> Narrow<U>()
        where U : class, TMessage => new(Id, (Message as U)!);
}
