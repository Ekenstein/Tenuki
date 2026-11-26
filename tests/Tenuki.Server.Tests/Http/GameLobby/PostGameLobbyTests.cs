using System.Net;
using Tenuki.Commons;
using Tenuki.Server.Http.Models;
using Tenuki.Server.Tests.Fixtures;
using Xunit;

namespace Tenuki.Server.Tests.Http.GameLobby;

public sealed class PostGameLobbyTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    [Fact]
    public async Task Post_Game_Lobby_Returns_201_Created_With_Id_And_Owner_Token()
    {
        using var response = await fixture.GameLobbies.PostGameLobbyAsync(
            GameLobbySettings.Default,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PostGameLobbyResponse>(
            JsonUtils.DefaultOptions,
            TestContext.Current.CancellationToken
        );
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id.Value);
        Assert.NotNull(created.OwnerToken);

        var location = response.Headers.Location;
        Assert.NotNull(location);

        var expectedLocation = $"/api/game-lobbies/{created.Id}";
        var actualLocation = location.IsAbsoluteUri
            ? location.AbsolutePath
            : location.OriginalString;

        Assert.Equal(expectedLocation, actualLocation);
    }
}
