namespace Tenuki.Services.Application;

public interface IConnectionPresence
{
    public Task RegisterAsync(string connectionId, CancellationToken cancellationToken);
    public Task<bool> UnregisterAsync(string connectionId, CancellationToken cancellationToken);
    public Task RefreshAsync(string connectionId, CancellationToken cancellationToken);
    public Task<bool> IsActiveAsync(string connectionId, CancellationToken cancellationToken);
}
