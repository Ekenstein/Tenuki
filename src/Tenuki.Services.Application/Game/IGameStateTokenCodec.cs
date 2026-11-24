using Tenuki.Commons;

namespace Tenuki.Services.Application.Game;

public interface IGameStateTokenCodec
{
    string Encode(GameState state);
    Result<GameState> Decode(string token);
}
