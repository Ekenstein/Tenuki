using Tenuki.Commons;
using Tenuki.Server.Http.Models;
using Tenuki.Services.Application.Game;
using Tenuki.Tests.Utils;

namespace Tenuki.Server.Tests.Clients;

public sealed class GameClient(HttpClient httpClient)
{
    public async Task<JsonHttpResponseMessage<GameCommandResponse>> PostGameCommandAsync(
        PostGameCommandRequest request,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await httpClient.PostAsJsonAsync(
            "api/games/command",
            request,
            JsonUtils.DefaultOptions,
            cancellationToken
        );
        return new(response);
    }

    public async Task<JsonHttpResponseMessage<GameDescription>> GetGameAsync(
        GameId gameId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await httpClient.GetAsync($"api/games/{gameId}", cancellationToken);
        return new(response);
    }

    public async Task<JsonHttpResponseMessage<GetGamesResponse>> GetLiveGamesAsync(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await httpClient.GetAsync("api/games/live", cancellationToken);
        return new(response);
    }
}
