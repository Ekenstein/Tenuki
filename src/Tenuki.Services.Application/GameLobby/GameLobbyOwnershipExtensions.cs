using Tenuki.Commons;

namespace Tenuki.Services.Application.GameLobby;

internal static class GameLobbyOwnershipExtensions
{
    extension(GameLobbyOwnership ownership)
    {
        internal bool IsOwner(SecretToken token) => token.Verify(ownership.Owner);

        internal bool IsChallengeOwner(ChallengeId challengeId, SecretToken token) =>
            ownership.Challengers.TryGetValue(challengeId, out var hash) && token.Verify(hash);

        internal GameLobbyOwnership AddChallenger(ChallengeId id, SecretToken token) =>
            ownership with
            {
                Challengers = ownership.Challengers.Add(id, token.ComputeHash()),
            };

        internal GameLobbyOwnership RemoveChallenger(ChallengeId id) =>
            ownership with
            {
                Challengers = ownership.Challengers.Remove(id),
            };

        internal GameLobbyOwnership ClearChallengers() =>
            ownership with
            {
                Challengers = ownership.Challengers.Clear(),
            };
    }
}
