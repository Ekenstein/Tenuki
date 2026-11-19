using System.Net;
using Tenuki.Commons;
using Tenuki.Server.Models;
using Tenuki.Services.Application;
using Tenuki.Tests.Utils;
using Xunit;

namespace Tenuki.Server.Tests.Fixtures;

public static class ServerFixtureExtensions
{
    extension(ServerFixture fixture)
    {
        public async Task<Result<GameDescription>> PostGameAsync(
            PostGameRequest request,
            CancellationToken cancellationToken
        )
        {
            using var response = await fixture.Games.PostGameAsync(request, cancellationToken);

            return await response.OkOrBadRequestAsync(cancellationToken);
        }

        public async Task<Result<GameCommandResponse>> PostGameCommandAsync(
            PostGameCommandRequest request,
            CancellationToken cancellationToken
        )
        {
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
            using var response = await fixture.Games.GetGameAsync(gameId, cancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.GetValueAsync(cancellationToken);
        }

        public async Task<GetGamesResponse> GetLiveGamesAsync(CancellationToken cancellationToken)
        {
            using var response = await fixture.Games.GetLiveGamesAsync(cancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.GetValueAsync(cancellationToken);
        }
    }
}
