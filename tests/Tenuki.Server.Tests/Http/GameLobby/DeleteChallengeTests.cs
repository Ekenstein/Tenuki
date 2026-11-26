using System.Collections.Immutable;
using System.Net;
using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Services.Logic.GameLobby;
using Tenuki.Tests.Utils;
using Xunit;

namespace Tenuki.Server.Tests.Http.GameLobby;

public sealed class DeleteChallengeTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    [Fact]
    public async Task Delete_Challenge_Returns_404_NotFound_For_A_Non_Existing_Game_Lobby()
    {
        using var response = await fixture.GameLobbies.DeleteChallengeAsync(
            GameId.NewId(),
            ChallengeId.NewId(),
            SecretToken.Generate().ToHexString(),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Challenge_Returns_400_BadRequest_If_Challenge_Could_Not_Be_Found()
    {
        var gameLobby = await fixture
            .PostGameLobbyAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        using var response = await fixture.GameLobbies.DeleteChallengeAsync(
            gameLobby.Id,
            ChallengeId.NewId(),
            gameLobby.OwnerToken,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errors = await response.Content.ReadFromJsonAsync<ImmutableList<Error>>(
            JsonUtils.DefaultOptions,
            TestContext.Current.CancellationToken
        );
        Assert.NotNull(errors);
        Assert.Contains(GameLobbyLogicErrors.ChallengeNotFound, errors);
    }

    [Fact]
    public async Task Delete_Challenge_Returns_403_Forbbiden_If_OwnerToken_Does_Not_Match_The_GameLobby_Owner_Or_The_Challenger_OwnerToken()
    {
        var gameLobby = await fixture
            .PostGameLobbyAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        var challenge = await fixture
            .PostChallengeAsync(
                gameLobby.Id,
                GameLobbySettings.Default,
                TestContext.Current.CancellationToken
            )
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        using var response = await fixture.GameLobbies.DeleteChallengeAsync(
            gameLobby.Id,
            challenge.ChallengeId,
            SecretToken.Generate().ToHexString(),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Challenge_Returns_401_Unauthorized_If_Token_Is_Not_Added_As_A_Bearer_Token()
    {
        var gameLobby = await fixture
            .PostGameLobbyAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        var challenge = await fixture
            .PostChallengeAsync(
                gameLobby.Id,
                GameLobbySettings.Default,
                TestContext.Current.CancellationToken
            )
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        using var response = await fixture.GameLobbies.DeleteChallengeAsync(
            gameLobby.Id,
            challenge.ChallengeId,
            null,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Challenge_Returns_204_NoContent_And_Notifies_GameLobby_Subscribers()
    {
        var gameLobby = await fixture
            .PostGameLobbyAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        await using var hubClient = fixture.CreateHubClient();
        await hubClient.StartAsync(TestContext.Current.CancellationToken);
        await hubClient.JoinGameLobbyAsync(gameLobby.Id, TestContext.Current.CancellationToken);

        var challenge = await fixture
            .PostChallengeAsync(
                gameLobby.Id,
                GameLobbySettings.Default,
                TestContext.Current.CancellationToken
            )
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        await fixture.ProcessIncomingMessagesAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(
            await hubClient.GameLobbyMessages.WaitForMessageAsync<GameLobbyMessage.StateUpdated>(
                message =>
                    message.GameId == gameLobby.Id
                    && message.State is GameLobbyState.Waiting waiting
                    && waiting.Challenges.Any(x => x.Id == challenge.ChallengeId),
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken
            )
        );

        using var response = await fixture.GameLobbies.DeleteChallengeAsync(
            gameLobby.Id,
            challenge.ChallengeId,
            gameLobby.OwnerToken,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await fixture.ProcessIncomingMessagesAsync(TestContext.Current.CancellationToken);

        var receivedMessages =
            await hubClient.GameLobbyMessages.WaitForMessageAsync<GameLobbyMessage.StateUpdated>(
                message =>
                    message.GameId == gameLobby.Id
                    && message.State is GameLobbyState.Waiting waiting
                    && !waiting.Challenges.Any(x => x.Id == challenge.ChallengeId),
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken
            );
        Assert.NotNull(receivedMessages);
    }
}
