namespace Tenuki.Server.Messaging;

public interface IMessageBroadcaster<in TMessage>
{
    Task BroadcastAsync(TMessage message, CancellationToken cancellationToken);
}
