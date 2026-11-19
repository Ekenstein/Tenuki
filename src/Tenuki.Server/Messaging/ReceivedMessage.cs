namespace Tenuki.Server.Messaging;

public static class ReceivedMessage
{
    public static ReceivedMessage<TId, TMessage> Create<TId, TMessage>(TId id, TMessage message) =>
        new(id, message);
}

public sealed record ReceivedMessage<TId, TMessage>(TId Id, TMessage Message)
{
    public ReceivedMessage<TId, U> Narrow<U>()
        where U : class, TMessage => new(Id, (Message as U)!);
}
