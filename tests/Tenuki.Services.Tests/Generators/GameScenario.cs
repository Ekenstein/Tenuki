using System.Collections.Immutable;
using Tenuki.Commons;
using Tenuki.Protocol;

namespace Tenuki.Services.Tests.Generators;

public sealed record GameScenario(GameSettings Settings, ImmutableList<GameCommand> Commands);
