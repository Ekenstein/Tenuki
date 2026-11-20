using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Tenuki.Commons;
using Tenuki.Services.Application;

namespace Tenuki.Server;

public sealed class GameStateHmacTokenCodec(
    IOptions<JsonOptions> jsonOptions,
    IOptions<HmacTokenOptions> hmacTokenOptions
) : IGameStateTokenCodec
{
    public Result<GameState> Decode(string token)
    {
        return from payload in HmacTokenCodec.Decode(token, hmacTokenOptions.Value.Secret)
            select JsonSerializer.Deserialize<GameState>(
                payload,
                jsonOptions.Value.SerializerOptions
            ) ?? throw new InvalidOperationException("Failed to deserialize the game state.");
    }

    public string Encode(GameState state)
    {
        var json = JsonSerializer.Serialize(state, jsonOptions.Value.SerializerOptions);
        return HmacTokenCodec.Encode(json, hmacTokenOptions.Value.Secret);
    }
}
