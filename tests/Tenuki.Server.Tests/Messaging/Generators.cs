using FsCheck;
using FsCheck.Fluent;
using System.Collections.Immutable;
using Tenuki.Commons;
using Tenuki.Protocol;
using Tenuki.Server.Messaging;

namespace Tenuki.Server.Tests.Messaging;

public static class Generators
{
    public static Gen<Point> PointGen =>
        from x in Gen.Choose(1, 19)
        from y in Gen.Choose(1, 19)
        select new Point(x, y);

    public static Gen<PlayerColor> PlayerColorGen => Gen.Elements(Enum.GetValues<PlayerColor>());
    public static Gen<GameId> GameIdGen =>
        ArbMap.Default.GeneratorFor<Guid>().Select(guid => new GameId(guid));

    public static Gen<CommandId> CommandIdGen =>
        ArbMap.Default.GeneratorFor<Guid>().Select(guid => new CommandId(guid));

    public static Gen<GameDescription> GameDescriptionGen =>
        from id in GameIdGen
        from token in ArbMap.Default.GeneratorFor<Guid>()
        from revision in Gen.Choose(1, 1000)
        select new GameDescription(id, token.ToString(), revision);

    public static Gen<IncomingMessage.GameAdded> GameAddedGen =>
        GameDescriptionGen.Select(x => new IncomingMessage.GameAdded(x));

    public static Gen<IncomingMessage.GameRemoved> GameRemovedGen =>
        GameIdGen.Select(x => new IncomingMessage.GameRemoved(x));

    public static Gen<IncomingMessage.GameChanged> GameChangedGen =>
        from game in GameDescriptionGen
        from commands in Gen.ListOf(GameCommandGen)
        select new IncomingMessage.GameChanged(game, [.. commands]);

    public static Gen<IncomingMessage> IncomingMessageGen =>
        Gen.OneOf(
            GameAddedGen.Select(x => (IncomingMessage)x),
            GameChangedGen.Select(x => (IncomingMessage)x),
            GameRemovedGen.Select(x => (IncomingMessage)x)
        );

    public static Gen<ImmutableList<IncomingMessage.GameChanged>> ConsecutiveGameChangesGen =>
        from gameId in GameIdGen
        from startRevision in Gen.Choose(1, 1000)
        from count in Gen.Choose(1, 20)
        from commands in Gen.ListOf(GameCommandGen, count)
        select Enumerable
            .Range(0, count)
            .Select(i => new IncomingMessage.GameChanged(
                new GameDescription(gameId, $"state-{startRevision + i}", startRevision + i),
                [commands[i]]
            ))
            .ToImmutableList();

    public static Gen<GameCommand.Pass> PassCommandGen =>
        from id in CommandIdGen
        from color in PlayerColorGen
        select new GameCommand.Pass(id, color);

    public static Gen<GameCommand.Resign> ResignCommandGen =>
        from id in CommandIdGen
        from color in PlayerColorGen
        select new GameCommand.Resign(id, color);

    public static Gen<GameCommand.Stone> StoneCommandGen =>
        from id in CommandIdGen
        from color in PlayerColorGen
        from point in PointGen
        select new GameCommand.Stone(id, color, point);

    public static Gen<GameCommand.RequestUndo> RequestUndoGen =>
        from id in CommandIdGen
        from color in PlayerColorGen
        select new GameCommand.RequestUndo(id, color);

    public static Gen<GameCommand.AcceptUndo> AcceptUndoGen =>
        from id in CommandIdGen
        from color in PlayerColorGen
        select new GameCommand.AcceptUndo(id, color);

    public static Gen<GameCommand.RejectUndo> RejectUndoGen =>
        from id in CommandIdGen
        from color in PlayerColorGen
        select new GameCommand.RejectUndo(id, color);

    public static Gen<GameCommand.MarkDeadStones> MarkDeadStonesGen =>
        from id in CommandIdGen
        from color in PlayerColorGen
        from points in Gen.ArrayOf(PointGen)
        select new GameCommand.MarkDeadStones(id, color, [.. points]);

    public static Gen<GameCommand.UnmarkDeadStones> UnmarkDeadStonesGen =>
        from id in CommandIdGen
        from color in PlayerColorGen
        from points in Gen.ArrayOf(PointGen)
        select new GameCommand.UnmarkDeadStones(id, color, [.. points]);

    public static Gen<GameCommand.AcceptScore> AcceptScoreGen =>
        from id in CommandIdGen
        from color in PlayerColorGen
        select new GameCommand.AcceptScore(id, color);

    public static Gen<GameCommand.ResumePlay> ResumePlayGen =>
        from id in CommandIdGen
        from color in PlayerColorGen
        select new GameCommand.ResumePlay(id, color);

    public static Gen<GameCommand> GameCommandGen =>
        Gen.OneOf(
            PassCommandGen.Select(x => (GameCommand)x),
            StoneCommandGen.Select(x => (GameCommand)x),
            ResignCommandGen.Select(x => (GameCommand)x),
            RequestUndoGen.Select(x => (GameCommand)x),
            AcceptUndoGen.Select(x => (GameCommand)x),
            RejectUndoGen.Select(x => (GameCommand)x),
            MarkDeadStonesGen.Select(x => (GameCommand)x),
            UnmarkDeadStonesGen.Select(x => (GameCommand)x),
            AcceptScoreGen.Select(x => (GameCommand)x),
            ResumePlayGen.Select(x => (GameCommand)x)
        );

    public static Gen<ReceivedMessage<Guid, IncomingMessage>> ReceivedMessageGen =>
        from id in ArbMap.Default.GeneratorFor<Guid>()
        from incomingMessage in IncomingMessageGen
        select ReceivedMessage.Create(id, incomingMessage);

    public static Arbitrary<ReceivedMessage<Guid, IncomingMessage>> ReceivedMessageArbitrary() =>
        Arb.From(ReceivedMessageGen);

    public static Arbitrary<ReceivedMessage<
        Guid,
        IncomingMessage.GameChanged
    >[]> GameChangedMessages()
    {
        var gen =
            from count in Gen.Choose(0, 50)
            from gameIds in Gen.ListOf(GameIdGen, 3)
            from revisions in Gen.ArrayOf(Gen.Choose(1, 20), count)
            from gameIndices in Gen.ArrayOf(Gen.Choose(0, 2), count)
            from stateTokens in Gen.ArrayOf(ArbMap.Default.GeneratorFor<Guid>(), count)
            from commands in Gen.ArrayOf(Gen.ListOf(GameCommandGen), count)
            select Enumerable
                .Range(0, count)
                .Select(i =>
                {
                    var game = new GameDescription(
                        gameIds[gameIndices[i]],
                        stateTokens[i].ToString("N"),
                        revisions[i]
                    );

                    return ReceivedMessage.Create(
                        Guid.NewGuid(),
                        new IncomingMessage.GameChanged(game, [.. commands[i]])
                    );
                })
                .ToArray();

        return Arb.From(gen);
    }
}
