using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Tenuki.Services.Application;

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record GameCommandResponse(string StateToken, long Revision)
{
    public sealed record Applied(string StateToken, long Revision) : GameCommandResponse(StateToken, Revision);
    public sealed record Rejected(string StateToken, long Revision, ImmutableList<Error> Errors) : GameCommandResponse(StateToken, Revision);
    public sealed record TimeExpired(string StateToken, long Revision) : GameCommandResponse(StateToken, Revision);
    public sealed record Conflict(string StateToken, long Revision) : GameCommandResponse(StateToken, Revision);
    public sealed record Count(string StateToken, long Revision) : GameCommandResponse(StateToken, Revision);
}
