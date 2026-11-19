using Tenuki.Commons;
using Tenuki.Server.Models;
using Tenuki.Services.Application;
using Tenuki.Tests.Utils;

namespace Tenuki.Server.Tests.Clients;

public sealed class GameClient(HttpClient httpClient)
{
    public async Task<JsonHttpResponseMessage<GameDescription>> PostGameAsync(
        PostGameRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/games",
            request,
            JsonUtils.DefaultOptions,
            cancellationToken
        );
        return new(response);
    }

    public async Task<JsonHttpResponseMessage<GameCommandResponse>> PostGameCommandAsync(
        PostGameCommandRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/games/command",
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
        var response = await httpClient.GetAsync($"api/games/{gameId.Value}", cancellationToken);
        return new(response);
    }

    public async Task<JsonHttpResponseMessage<GetGamesResponse>> GetLiveGamesAsync(
        CancellationToken cancellationToken
    )
    {
        var response = await httpClient.GetAsync("api/games/live", cancellationToken);
        return new(response);
    }
}
