using Tenuki.Commons;

namespace Tenuki.Services.Tests.Generators;

public sealed record KoScenario(GameState State, Stone Stone, DateTimeOffset Now);
