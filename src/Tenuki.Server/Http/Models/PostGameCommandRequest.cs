using Tenuki.Protocol;

namespace Tenuki.Server.Http.Models;

public sealed record PostGameCommandRequest(string StateToken, GameCommand Command);
