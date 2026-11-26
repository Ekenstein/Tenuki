using System.Net;
using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Server.Http.Models;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Services.Logic.Game;
using Tenuki.Tests.Utils;
using Xunit;

namespace Tenuki.Server.Tests.Http.GameLobby;

public sealed class PostChallengeTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    [Fact]
    public async Task Post_Challenge_Returns_404_NotFound_For_Non_Existing_GameLobby()
    {
        using var response = await fixture.GameLobbies.PostChallengeAsync(
            GameId.NewId(),
            GameLobbySettings.Default,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Challenge_Returns_400_BadRequest_For_Invalid_GameSettings()
    {
        var invalidGameSettings = GameSettings.Default with { BoardSize = -1 };

        var proposedGameLobbySettings = GameLobbySettings.Default with
        {
            GameSettings = invalidGameSettings,
        };

        var gameLobby = await fixture
            .PostGameLobbyAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        using var response = await fixture.GameLobbies.PostChallengeAsync(
            gameLobby.Id,
            proposedGameLobbySettings,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(
            GameLogicErrors.InvalidBoardSize,
            await response.GetErrorsAsync(TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task Post_Challenge_Returns_200_Ok_And_Notifies_GameLobby_Subscribers()
    {
        await using var hubClient = fixture.CreateHubClient();
        await hubClient.StartAsync(TestContext.Current.CancellationToken);

        var gameLobby = await fixture
            .PostGameLobbyAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        await hubClient.JoinGameLobbyAsync(gameLobby.Id, TestContext.Current.CancellationToken);

        using var response = await fixture.GameLobbies.PostChallengeAsync(
            gameLobby.Id,
            GameLobbySettings.Default,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var challenge = await response.Content.ReadFromJsonAsync<PostGameChallengeResponse>(
            JsonUtils.DefaultOptions,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(challenge);
        Assert.NotEqual(Guid.Empty, challenge.ChallengeId.Value);

        await fixture.ProcessIncomingMessagesAsync(TestContext.Current.CancellationToken);

        var receivedMessages =
            await hubClient.GameLobbyMessages.WaitForMessageAsync<GameLobbyMessage.StateUpdated>(
                message =>
                    message.State is GameLobbyState.Waiting waiting
                    && waiting.Challenges.Any(x => x.Id == challenge.ChallengeId),
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(receivedMessages);
    }
}
