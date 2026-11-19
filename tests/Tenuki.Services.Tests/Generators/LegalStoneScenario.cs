using Tenuki.Commons;
using Tenuki.Protocol;

namespace Tenuki.Services.Tests.Generators;

public sealed record LegalStoneScenario(
    GameState State,
    GameCommand.Stone Command,
    DateTimeOffset Now
);
