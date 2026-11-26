using Tenuki.Commons;
using Tenuki.Server.Http.Models;
using Tenuki.Tests.Utils;

namespace Tenuki.Server.Tests.Clients;

public sealed class GameLobbyClient(HttpClient httpClient)
{
    public async Task<JsonHttpResponseMessage<PostGameLobbyResponse>> PostGameLobbyAsync(
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
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/game-lobbies/{gameId}");

        var response = await httpClient.SendAsync(request, cancellationToken);

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
        string? ownerToken,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"api/game-lobbies/{gameId}/challenge/{challengeId}"
        );
        if (!string.IsNullOrWhiteSpace(ownerToken))
        {
            AddOwnerToken(request, ownerToken);
        }

        return await httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<JsonHttpResponseMessage<GameDescription>> PostAcceptChallengeAsync(
        GameId gameId,
        ChallengeId challengeId,
        string? ownerToken,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/game-lobbies/{gameId}/challenge/{challengeId}/accept"
        );
        if (!string.IsNullOrWhiteSpace(ownerToken))
        {
            AddOwnerToken(request, ownerToken);
        }

        var response = await httpClient.SendAsync(request, cancellationToken);

        return new(response);
    }

    private static void AddOwnerToken(HttpRequestMessage request, string ownerToken)
    {
        request.Headers.Authorization = new("Bearer", ownerToken);
    }
}
