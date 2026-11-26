using System.Net;
using Tenuki.Commons;
using Tenuki.Server.Http.Models;
using Tenuki.Services.Application.Game;
using Tenuki.Tests.Utils;
using Xunit;

namespace Tenuki.Server.Tests.Fixtures;

public static class ServerFixtureExtensions
{
    extension(ServerFixture fixture)
    {
        public async Task<Result<GameDescription>> CreateGameAsync(
            GameLobbySettings settings,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lobby = await fixture.PostGameLobbyAsync(settings, cancellationToken);
            if (!lobby.TryGetValue(out var lobbyState))
            {
                return new Failure(lobby.Errors);
            }

            var challenge = await fixture.PostChallengeAsync(
                lobbyState.Id,
                settings,
                cancellationToken
            );
            if (!challenge.TryGetValue(out var challengeState))
            {
                return new Failure(lobby.Errors);
            }

            return await fixture.PostAcceptChallengeAsync(
                lobbyState.Id,
                challengeState.ChallengeId,
                lobbyState.OwnerToken,
                cancellationToken
            );
        }

        public async Task<Result<GameDescription>> PostAcceptChallengeAsync(
            GameId gameId,
            ChallengeId challengeId,
            string? ownerToken,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var response = await fixture.GameLobbies.PostAcceptChallengeAsync(
                gameId,
                challengeId,
                ownerToken,
                cancellationToken
            );
            return await response.SuccessOrBadRequestAsync(
                HttpStatusCode.Created,
                cancellationToken
            );
        }

        public async Task<Result<PostGameChallengeResponse>> PostChallengeAsync(
            GameId gameId,
            GameLobbySettings challenge,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var response = await fixture.GameLobbies.PostChallengeAsync(
                gameId,
                challenge,
                cancellationToken
            );
            return await response.OkOrBadRequestAsync(cancellationToken);
        }

        public async Task<Result<PostGameLobbyResponse>> PostGameLobbyAsync(
            GameLobbySettings settings,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var response = await fixture.GameLobbies.PostGameLobbyAsync(
                settings,
                cancellationToken
            );

            return await response.SuccessOrBadRequestAsync(
                HttpStatusCode.Created,
                cancellationToken
            );
        }

        public async Task<Result<GameCommandResponse>> PostGameCommandAsync(
            PostGameCommandRequest request,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var response = await fixture.Games.PostGameCommandAsync(
                request,
                cancellationToken
            );

            return await response.OkOrBadRequestAsync(cancellationToken);
        }

        public async Task<GameDescription> GetGameAsync(
            GameId gameId,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var response = await fixture.Games.GetGameAsync(gameId, cancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.GetValueAsync(cancellationToken);
        }

        public async Task<GetGamesResponse> GetLiveGamesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var response = await fixture.Games.GetLiveGamesAsync(cancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.GetValueAsync(cancellationToken);
        }
    }
}
