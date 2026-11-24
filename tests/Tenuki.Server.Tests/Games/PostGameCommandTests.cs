using System.Net;
using System.Text;
using System.Text.Json;
using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Server.Http.Models;
using Tenuki.Server.Tests.Fixtures;
using Tenuki.Services.Application.Game;
using Tenuki.Services.Logic.Game;
using Tenuki.Services.Logic.GameLobby;
using Tenuki.Tests.Utils;
using Xunit;

namespace Tenuki.Server.Tests.Games;

public sealed class PostGameCommandTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    [Fact]
    public async Task Post_Game_Command_With_Tampered_State_Returns_400()
    {
        var response = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        var gameState = HmacTokenCodec
            .GetPayload(response.StateToken)
            .Let(x => JsonSerializer.Deserialize<GameState>(x));
        Assert.NotNull(gameState);

        var tamperedState = gameState with { Revision = gameState.Revision + 1 };
        var signature = HmacTokenCodec.GetSignature(response.StateToken);

        var tamperedStateJson = JsonSerializer.Serialize(tamperedState);
        var tamperedStateBytes = Encoding.UTF8.GetBytes(tamperedStateJson);
        var tamperedStateBase64 = Convert.ToBase64String(tamperedStateBytes);

        var tamperedStateToken = $"{tamperedStateBase64}.{signature}";

        var command = new GameCommand.Stone(new(), PlayerColor.Black, new(3, 3));

        using var commandResponse = await fixture.Games.PostGameCommandAsync(
            new(tamperedStateToken, command),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.BadRequest, commandResponse.StatusCode);

        var errors = await commandResponse.GetErrorsAsync(TestContext.Current.CancellationToken);
        Assert.Contains(HmacTokenCodec.InvalidTokenSignature, errors);
    }

    [Fact]
    public async Task Post_Game_Command_With_Invalid_Token_Format_Returns_400()
    {
        const string invalidToken = "Hello World";
        await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        var command = new GameCommand.Pass(new(), PlayerColor.Black);

        using var commandResponse = await fixture.Games.PostGameCommandAsync(
            new(invalidToken, command),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.BadRequest, commandResponse.StatusCode);

        var errors = await commandResponse.GetErrorsAsync(TestContext.Current.CancellationToken);
        Assert.Contains(HmacTokenCodec.InvalidTokenFormat, errors);
    }

    [Fact]
    public async Task Post_Game_Command_Pass_When_It_Is_Not_Players_Turn_Returns_200_With_Rejected_Outcome()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        var command = new GameCommand.Pass(new(), PlayerColor.White);

        var outcome = await fixture
            .PostGameCommandAsync(
                new(game.StateToken, command),
                TestContext.Current.CancellationToken
            )
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        var rejectedOutcome = Assert.IsType<GameCommandResponse.Rejected>(outcome);
        Assert.Contains(GameLogicErrors.NotPlayerTurn, rejectedOutcome.Errors);
    }

    [Fact]
    public async Task Post_Game_Command_Stone_When_It_Is_Not_Players_Turn_Returns_200_With_Rejected_Outcome()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        var command = new GameCommand.Stone(new(), PlayerColor.White, new(3, 3));

        var outcome = await fixture
            .PostGameCommandAsync(
                new(game.StateToken, command),
                TestContext.Current.CancellationToken
            )
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        var rejectedOutcome = Assert.IsType<GameCommandResponse.Rejected>(outcome);
        Assert.Contains(GameLogicErrors.NotPlayerTurn, rejectedOutcome.Errors);
    }

    [Fact]
    public async Task Post_Game_Command_When_Time_Has_Run_Out_Returns_200_With_TimeExpired_Outcome()
    {
        var request = GameLobbySettings.Default with
        {
            GameSettings = GameLobbySettings.Default.GameSettings with
            {
                TimeSettings = new TimeSettings.Fixed(60_000L),
            },
        };

        var game = await fixture
            .CreateGameAsync(request, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        fixture.TimeProvider.Advance(
            TimeSpan.FromMilliseconds(request.GameSettings.TimeSettings.MainTimeMs)
        );

        var command = new GameCommand.Pass(new(), PlayerColor.Black);
        var outcome = await fixture
            .PostGameCommandAsync(
                new(game.StateToken, command),
                TestContext.Current.CancellationToken
            )
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        Assert.IsType<GameCommandResponse.TimeExpired>(outcome);
    }

    [Fact]
    public async Task Post_Game_Command_Stone_On_Occupied_Point_Returns_200_With_Rejected_Outcome()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        var c1 = new GameCommand.Stone(new(), PlayerColor.Black, new(3, 3));
        var firstMove = Assert.IsType<GameCommandResponse.Applied>(
            await fixture
                .PostGameCommandAsync(
                    new(game.StateToken, c1),
                    TestContext.Current.CancellationToken
                )
                .AssertNoFailureAsync(TestContext.Current.CancellationToken)
        );

        var c2 = new GameCommand.Stone(new(), c1.Color.Opponent, c1.Point);
        var secondMove = Assert.IsType<GameCommandResponse.Rejected>(
            await fixture
                .PostGameCommandAsync(
                    new(firstMove.StateToken, c2),
                    TestContext.Current.CancellationToken
                )
                .AssertNoFailureAsync(TestContext.Current.CancellationToken)
        );

        Assert.Contains(GameLogicErrors.PointOccupied, secondMove.Errors);
    }

    [Fact]
    public async Task Post_Game_Command_Stone_That_Captures_Another_Stone_Increases_Captures()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        GameCommand[] setupPosition =
        [
            new GameCommand.Stone(new(), PlayerColor.Black, new(0, 0)),
            new GameCommand.Stone(new(), PlayerColor.White, new(0, 1)),
            new GameCommand.Pass(new(), PlayerColor.Black),
        ];

        // setup position
        var stateToken = await SetupPositionAsync(game.StateToken, setupPosition);

        // capture (0,0) black stone.
        var command = new GameCommand.Stone(new(), PlayerColor.White, new(1, 0));
        var request = new PostGameCommandRequest(stateToken, command);
        var outcome = Assert.IsType<GameCommandResponse.Applied>(
            await fixture
                .PostGameCommandAsync(request, TestContext.Current.CancellationToken)
                .AssertNoFailureAsync(TestContext.Current.CancellationToken)
        );

        var gameState = fixture.DecodeStateToken(outcome.StateToken);
        Assert.Equal(1, gameState.CurrentBoard.WhiteCaptures);
    }

    [Fact]
    public async Task Post_Game_Command_With_Out_Of_Date_State_Returns_Conflict_With_Current_StateToken()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        var appliedOutcome = Assert.IsType<GameCommandResponse.Applied>(
            await fixture
                .PostGameCommandAsync(
                    new(
                        game.StateToken,
                        new GameCommand.Stone(new(), PlayerColor.Black, new(3, 3))
                    ),
                    TestContext.Current.CancellationToken
                )
                .AssertNoFailureAsync(TestContext.Current.CancellationToken)
        );

        // Post command towards an out-of-date state.
        var conflictOutcome = Assert.IsType<GameCommandResponse.Conflict>(
            await fixture
                .PostGameCommandAsync(
                    new(
                        game.StateToken,
                        new GameCommand.Stone(new(), PlayerColor.Black, new(3, 3))
                    ),
                    TestContext.Current.CancellationToken
                )
                .AssertNoFailureAsync(TestContext.Current.CancellationToken)
        );
        Assert.Equal(appliedOutcome.StateToken, conflictOutcome.StateToken);
        Assert.Equal(appliedOutcome.Revision, conflictOutcome.Revision);
    }

    [Fact]
    public async Task Post_Game_Command_Broadcasts_State_Update_To_Subscribers()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);
        await using var client1 = fixture.CreateHubClient();
        await client1.StartAsync(TestContext.Current.CancellationToken);
        await client1.JoinGameAsync(game.Id, TestContext.Current.CancellationToken);

        var command = new GameCommand.Stone(new(), PlayerColor.Black, new(3, 3));
        var outcome = Assert.IsType<GameCommandResponse.Applied>(
            await fixture
                .PostGameCommandAsync(
                    new(game.StateToken, command),
                    TestContext.Current.CancellationToken
                )
                .AssertNoFailureAsync(TestContext.Current.CancellationToken)
        );

        await fixture.ProcessIncomingMessagesAsync(TestContext.Current.CancellationToken);

        var receivedMessage =
            await client1.GameMessages.WaitForMessageAsync<GameMessage.StateUpdated>(
                x => x.GameId == game.Id && x.StateToken == outcome.StateToken,
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(receivedMessage);
    }

    [Fact]
    public async Task Post_Game_Commands_Broadcasts_Batched_State_Update_With_Revision_Range()
    {
        var game = await fixture
            .CreateGameAsync(GameLobbySettings.Default, TestContext.Current.CancellationToken)
            .AssertNoFailureAsync(TestContext.Current.CancellationToken);

        await using var client = fixture.CreateHubClient();
        await client.StartAsync(TestContext.Current.CancellationToken);
        await client.JoinGameAsync(game.Id, TestContext.Current.CancellationToken);

        GameCommand[] commands =
        [
            new GameCommand.Stone(new(), PlayerColor.Black, new(3, 3)),
            new GameCommand.Stone(new(), PlayerColor.White, new(15, 15)),
            new GameCommand.Stone(new(), PlayerColor.Black, new(3, 4)),
        ];

        var stateToken = game.StateToken;

        foreach (var command in commands)
        {
            var outcome = Assert.IsType<GameCommandResponse.Applied>(
                await fixture
                    .PostGameCommandAsync(
                        new PostGameCommandRequest(stateToken, command),
                        TestContext.Current.CancellationToken
                    )
                    .AssertNoFailureAsync(TestContext.Current.CancellationToken)
            );

            stateToken = outcome.StateToken;
        }

        // Process all three notifications in a single batch.
        await fixture.ProcessIncomingMessagesAsync(TestContext.Current.CancellationToken);

        var receivedMessage =
            await client.GameMessages.WaitForMessageAsync<GameMessage.StateUpdated>(
                x => x.GameId == game.Id && x.StateToken == stateToken,
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(receivedMessage);
        var (message, _) = receivedMessage;

        Assert.Equal(1, message.PreviousRevision);
        Assert.Equal(4, message.Revision);
        Assert.Equal(commands, message.CommittedCommands);
    }

    private async Task<string> SetupPositionAsync(
        string initialStateToken,
        GameCommand[] setupPosition
    )
    {
        string stateToken = initialStateToken;
        foreach (var command in setupPosition)
        {
            var request = new PostGameCommandRequest(stateToken, command);
            var outcome = Assert.IsType<GameCommandResponse.Applied>(
                await fixture
                    .PostGameCommandAsync(request, TestContext.Current.CancellationToken)
                    .AssertNoFailureAsync(TestContext.Current.CancellationToken)
            );
            stateToken = outcome.StateToken;
        }
        return stateToken;
    }
}
