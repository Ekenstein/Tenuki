using System.Collections.Immutable;
using Tenuki.Commons;

namespace Tenuki.Server.Http.Models;

public sealed record GetGamesResponse(ImmutableList<GameDescription> Games);
