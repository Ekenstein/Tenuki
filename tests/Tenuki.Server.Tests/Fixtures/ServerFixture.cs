using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Tenuki.Commons;
using Tenuki.Server.BackgroundServices;
using Tenuki.Server.Options;
using Tenuki.Server.Redis;
using Tenuki.Server.Tests.Clients;
using Tenuki.Services.Application;
using Tenuki.Tests.Utils;

namespace Tenuki.Server.Tests.Fixtures;

public sealed class ServerFixture : IAsyncDisposable
{
    private readonly ServerFactory _serverFactory;
    private readonly HttpClient _httpClient;

    public GameClient Games { get; }
    public FakeTimeProvider TimeProvider { get; } = new FakeTimeProvider();
    public GameNotificationOptions GameNotificationOptions =>
        _serverFactory.Services.GetRequiredService<IOptions<GameNotificationOptions>>().Value;

    public ServerFixture()
    {
        _serverFactory = new ServerFactory(TimeProvider, ServerFactory.ValidConfig);
        _httpClient = _serverFactory.CreateClient();
        Games = new GameClient(_httpClient);
    }

    public TenukiClient CreateHubClient()
    {
        var uri = new UriBuilder
        {
            Host = "localhost",
            Scheme = "http",
            Path = "/api/hub",
        }.Uri;

        var connection = new HubConnectionBuilder()
            .AddJsonProtocol(options =>
                JsonUtils.ConfigureOptions(options.PayloadSerializerOptions)
            )
            .WithUrl(uri, transport =>
            {
                transport.Transports = HttpTransportType.LongPolling;
                transport.HttpMessageHandlerFactory = _ => _serverFactory.Server.CreateHandler();
            })
            .Build();

        return new TenukiClient(connection);
    }

    public Task WaitForBackgroundServiceToStartAsync<T>(CancellationToken cancellationToken)
        where T : TimedBackgroundService =>
        GetTimedBackgroundService<T>().WaitForTimerToStartAsync(cancellationToken);

    public async Task AdvanceBackgroundServiceAsync<T>(
        long tick,
        CancellationToken cancellationToken
    )
        where T : TimedBackgroundService
    {
        var service = GetTimedBackgroundService<T>();
        TimeProvider.Advance(service.Period);
        await service.WaitForTickAsync(tick, cancellationToken);
    }

    private T GetTimedBackgroundService<T>()
        where T : TimedBackgroundService =>
        _serverFactory.Services.GetServices<IHostedService>().OfType<T>().SingleOrDefault()
        ?? throw new InvalidOperationException(
            $"There's no hosted service of type '{typeof(T).Name}'."
        );

    public async Task ProcessIncomingMessagesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reader = _serverFactory.Services.GetRequiredService<RedisIncomingMessageReader>();

        await reader.InitializeAsync(cancellationToken);
        await reader.ReadIncomingMessagesAsync("tenuki-test-client", cancellationToken);
    }

    public async Task RefreshConnectionPresenceAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var refresher = _serverFactory.Services.GetRequiredService<ConnectionPresenceRefresher>();
        await refresher.RefreshAllConnectionsAsync(cancellationToken);
    }

    public async Task ExpireConnectionPresenceAsync(
        string connectionId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var presence = _serverFactory.Services.GetRequiredService<IConnectionPresence>();
        await presence.UnregisterAsync(connectionId, cancellationToken);
    }

    public GameState DecodeStateToken(string stateToken)
    {
        var tokenCodec = _serverFactory.Services.GetRequiredService<IGameStateTokenCodec>();
        return tokenCodec.Decode(stateToken).AssertNoFailure();
    }

    public async ValueTask DisposeAsync()
    {
        await _serverFactory.DisposeAsync();
        _httpClient.Dispose();
    }
}
