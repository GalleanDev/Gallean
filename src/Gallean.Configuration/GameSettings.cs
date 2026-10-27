using System.Text.Json.Serialization;

namespace Gallean.Configuration;

public sealed record GameSettings(
    [property: JsonPropertyOrder(-1)] int FormatVersion,
    int SoundVolume,
    int MusicVolume,
    bool Windowed,
    int RenderResolution,
    int AutoSaveInterval = GameSettings.DefaultAutoSaveInterval,
    int AutoSaveLimit = GameSettings.DefaultAutoSaveLimit,
    BattleSpeed BattleSpeed = GameSettings.DefaultBattleSpeed,
    UiSize UiSize = GameSettings.DefaultUiSize,
    bool LockCursor = true,
    MovementSpeed MovementSpeed = GameSettings.DefaultMovementSpeed)
{
    public const int CurrentFormatVersion = 6;
    public const int MinVolume = 0;
    public const int MaxVolume = 100;
    public const int MinRenderResolution = 50;
    public const int MaxRenderResolution = 100;
    public const int DefaultAutoSaveInterval = 5;
    public const int MinAutoSaveLimit = 1;
    public const int DefaultAutoSaveLimit = 10;
    public const BattleSpeed DefaultBattleSpeed = BattleSpeed.Normal;
    public const MovementSpeed DefaultMovementSpeed = MovementSpeed.Normal;
    public const UiSize DefaultUiSize = UiSize.Normal;

    public static GameSettings Default { get; } = Create(
        MaxVolume,
        MaxVolume,
        false,
        MaxRenderResolution,
        DefaultAutoSaveInterval,
        DefaultAutoSaveLimit,
        DefaultBattleSpeed,
        DefaultUiSize,
        true,
        DefaultMovementSpeed);

    public int SoundVolume { get; } = Math.Clamp(SoundVolume, MinVolume, MaxVolume);
    public int MusicVolume { get; } = Math.Clamp(MusicVolume, MinVolume, MaxVolume);
    public int RenderResolution { get; } = Math.Clamp(RenderResolution, MinRenderResolution, MaxRenderResolution);
    public int AutoSaveInterval { get; } = Math.Max(AutoSaveInterval, 0);
    public int AutoSaveLimit { get; } = Math.Max(AutoSaveLimit, MinAutoSaveLimit);

    public static GameSettings Create(
        int soundVolume,
        int musicVolume,
        bool windowed,
        int renderResolution,
        int autoSaveInterval,
        int autoSaveLimit,
        BattleSpeed battleSpeed,
        UiSize uiSize,
        bool lockCursor,
        MovementSpeed movementSpeed)
    {
        return new GameSettings(
            CurrentFormatVersion,
            soundVolume,
            musicVolume,
            windowed,
            renderResolution,
            autoSaveInterval,
            autoSaveLimit,
            battleSpeed,
            uiSize,
            lockCursor,
            movementSpeed);
    }
}
