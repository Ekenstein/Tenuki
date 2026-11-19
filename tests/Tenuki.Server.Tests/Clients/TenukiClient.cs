using System.Collections.Immutable;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Client;
using Tenuki.Commons;
using Tenuki.Protocol;
using Xunit;

namespace Tenuki.Server.Tests.Clients;

public sealed class TenukiClient : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly IDisposable _gameSubscription;
    private readonly IDisposable _lobbySubscription;
    public ReceivedMessages<GameMessage> GameMessages { get; } = new();
    public ReceivedMessages<LobbyMessage> LobbyMessages { get; } = new();

    public string ConnectionId =>
        _hubConnection.ConnectionId
        ?? throw new InvalidOperationException("Hub connection is not started yet.");

    public TenukiClient(HubConnection hubConnection)
    {
        _hubConnection = hubConnection;
        _gameSubscription = hubConnection.On<GameMessage>(
            nameof(ITenukiClient.GameMessage),
            message => GameMessages.OnMessageReceivedAsync(message).AsTask()
        );
        _lobbySubscription = hubConnection.On<LobbyMessage>(
            nameof(ITenukiClient.LobbyMessage),
            message => LobbyMessages.OnMessageReceivedAsync(message).AsTask()
        );
    }

    public Task StartAsync(CancellationToken cancellationToken) =>
        _hubConnection.StartAsync(cancellationToken);

    public Task JoinGameAsync(GameId gameId, CancellationToken cancellationToken) =>
        _hubConnection.InvokeAsync(nameof(TenukiHub.JoinGame), gameId, cancellationToken);

    public Task JoinLobbyAsync(CancellationToken cancellationToken) =>
        _hubConnection.InvokeAsync(nameof(TenukiHub.JoinLobby), cancellationToken);

    public async ValueTask DisposeAsync()
    {
        _gameSubscription.Dispose();
        _lobbySubscription.Dispose();

        await _hubConnection.DisposeAsync();
    }
}

public sealed class ReceivedMessages<T>
    where T : class
{
    private readonly SemaphoreSlim _lock = new(1);
    private readonly List<T> _messages = [];
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
    );

    public ValueTask OnMessageReceivedAsync(T message)
    {
        return _channel.Writer.WriteAsync(message, TestContext.Current.CancellationToken);
    }

    public async Task<ReceivedMessage<T, U>?> WaitForMessageAsync<U>(
        Predicate<U> predicate,
        CancellationToken cancellationToken
    )
        where U : class, T
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        var prefix = ImmutableList.CreateBuilder<T>();

        try
        {
            while (true)
            {
                var message = await _channel.Reader.ReadAsync(timeout.Token);

                if (message is U typedMessage && predicate(typedMessage))
                {
                    return new(typedMessage, prefix.ToImmutable());
                }

                prefix.Add(message);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}

public sealed record ReceivedMessage<T, U>(U Message, ImmutableList<T> Prefix)
    where U : T;
