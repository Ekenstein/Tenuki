using Tenuki.Commons;
using Tenuki.Server.Http.Models;
using Tenuki.Services.Logic.GameLobby;
using Tenuki.Tests.Utils;

namespace Tenuki.Server.Tests.Clients;

public sealed class GameLobbyClient(HttpClient httpClient)
{
    public async Task<JsonHttpResponseMessage<GameLobbyState.Waiting>> PostGameLobbyAsync(
        GameLobbySettings settings,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await httpClient.PostAsJsonAsync(
            "api/game-lobbies",
            settings,
            JsonUtils.DefaultOptions,
            cancellationToken
        );
        return new(response);
    }

    public async Task<JsonHttpResponseMessage<GameLobbyState>> GetGameLobbyAsync(
        GameId gameId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await httpClient.GetAsync($"api/game-lobbies/{gameId}", cancellationToken);

        return new(response);
    }

    public async Task<JsonHttpResponseMessage<PostGameChallengeResponse>> PostChallengeAsync(
        GameId gameId,
        GameLobbySettings challenge,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await httpClient.PostAsJsonAsync(
            $"api/game-lobbies/{gameId}/challenge",
            challenge,
            JsonUtils.DefaultOptions,
            cancellationToken
        );

        return new(response);
    }

    public async Task<HttpResponseMessage> DeleteChallengeAsync(
        GameId gameId,
        ChallengeId challengeId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await httpClient.DeleteAsync(
            $"api/game-lobbies/{gameId}/challenge/{challengeId}",
            cancellationToken
        );
    }

    public async Task<JsonHttpResponseMessage<GameDescription>> PostAcceptChallengeAsync(
        GameId gameId,
        ChallengeId challengeId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await httpClient.PostAsync(
            $"api/game-lobbies/{gameId}/challenge/{challengeId}/accept",
            content: null,
            cancellationToken
        );

        return new(response);
    }
}
