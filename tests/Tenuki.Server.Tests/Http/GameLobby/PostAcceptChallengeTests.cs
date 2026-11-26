using System.Net;
using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Services.Logic.GameLobby;
using Tenuki.Tests.Utils;
using Xunit;

namespace Tenuki.Server.Tests.Http.GameLobby;

public sealed class PostAcceptChallengeTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    [Fact]
    public async Task Post_Accept_Challenge_Returns_404_NotFound_For_Non_Existing_GameLobby()
    {
        using var response = await fixture.GameLobbies.PostAcceptChallengeAsync(
            GameId.NewId(),
            ChallengeId.NewId(),
            SecretToken.Generate().ToHexString(),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Accept_Challenge_Returns_400_BadRequest_For_Non_Existing_Challenge()
    {
        var game = await fixture
            .PostGameLobbyAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        using var response = await fixture.GameLobbies.PostAcceptChallengeAsync(
            game.Id,
            ChallengeId.NewId(),
            game.OwnerToken,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(
            GameLobbyLogicErrors.ChallengeNotFound,
            await response.GetErrorsAsync(TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task Post_Accept_Challenge_Returns_403_Forbidden_If_OwnerToken_Does_Not_Belong_To_Game_Owner()
    {
        var game = await fixture
            .PostGameLobbyAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        var challenge = await fixture
            .PostChallengeAsync(
                game.Id,
                GameLobbySettings.Default,
                TestContext.Current.CancellationToken
            )
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        using var response = await fixture.GameLobbies.PostAcceptChallengeAsync(
            game.Id,
            challenge.ChallengeId,
            challenge.OwnerToken,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Accept_Challenge_Returns_401_Unauthorized_If_OwnerToken_Is_Not_In_Authorization_Headers()
    {
        var game = await fixture
            .PostGameLobbyAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        var challenge = await fixture
            .PostChallengeAsync(
                game.Id,
                GameLobbySettings.Default,
                TestContext.Current.CancellationToken
            )
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        using var response = await fixture.GameLobbies.PostAcceptChallengeAsync(
            game.Id,
            challenge.ChallengeId,
            null,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Accept_Challenge_Returns_201_Created_And_Notifies_Lobby_Subscibers()
    {
        var game = await fixture
            .PostGameLobbyAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        var challenge = await fixture
            .PostChallengeAsync(
                game.Id,
                GameLobbySettings.Default,
                TestContext.Current.CancellationToken
            )
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        await using var hubClient = fixture.CreateHubClient();
        await hubClient.StartAsync(TestContext.Current.CancellationToken);
        await hubClient.JoinLobbyAsync(TestContext.Current.CancellationToken);

        using var response = await fixture.GameLobbies.PostAcceptChallengeAsync(
            game.Id,
            challenge.ChallengeId,
            game.OwnerToken,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var gameDescription = await response.Content.ReadFromJsonAsync<GameDescription>(
            JsonUtils.DefaultOptions,
            TestContext.Current.CancellationToken
        );
        Assert.NotNull(gameDescription);
        Assert.Equal(game.Id, gameDescription.Id);
        Assert.Equal(1, gameDescription.Revision);

        var location = response.Headers.Location;
        Assert.NotNull(location);

        var expectedLocation = $"/api/games/{gameDescription.Id}";
        var actualLocation = location.IsAbsoluteUri
            ? location.AbsolutePath
            : location.OriginalString;

        Assert.Equal(expectedLocation, actualLocation);

        await fixture.ProcessIncomingMessagesAsync(TestContext.Current.CancellationToken);

        var receivedMessages =
            await hubClient.LobbyMessages.WaitForMessageAsync<LobbyMessage.GamesAdded>(
                message => message.Games.Contains(gameDescription),
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(receivedMessages);
    }
}
