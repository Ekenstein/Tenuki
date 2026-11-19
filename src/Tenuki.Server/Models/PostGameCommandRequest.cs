using Tenuki.Protocol;

namespace Tenuki.Server.Models;

public sealed record PostGameCommandRequest(string StateToken, GameCommand Command);
