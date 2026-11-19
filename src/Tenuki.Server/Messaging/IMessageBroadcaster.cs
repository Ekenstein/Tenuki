namespace Tenuki.Server.Messaging;

public interface IMessageBroadcaster<TMessage>
{
    Task BroadcastAsync(TMessage message, CancellationToken cancellationToken);
}
