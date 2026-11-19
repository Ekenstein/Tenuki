using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Server.Models;

public sealed record GetGamesResponse(ImmutableList<GameDescription> Games);
