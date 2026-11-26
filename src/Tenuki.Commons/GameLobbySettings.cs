namespace Tenuki.Commons;

public sealed record GameLobbySettings(GameSettings GameSettings, ColorPreference ColorPreference)
{
    public static readonly GameLobbySettings Default = new(
        GameSettings.Default,
        ColorPreference.Random
    );
};
