using System.Text.Json.Serialization;
using Gallean.Engine.Contracts;

namespace Gallean.Configuration;

public sealed record GameSettings(
    [property: JsonPropertyOrder(-1)] int FormatVersion,
    bool Windowed,
    int RenderResolution,
    int AutoSaveInterval = GameSettings.DefaultAutoSaveInterval,
    int AutoSaveLimit = GameSettings.DefaultAutoSaveLimit,
    BattleSpeed BattleSpeed = GameSettings.DefaultBattleSpeed,
    UiSize UiSize = GameSettings.DefaultUiSize,
    bool LockCursor = true,
    MovementSpeed MovementSpeed = GameSettings.DefaultMovementSpeed,
    Language? Language = null,
    BattlePosition BattlePosition = GameSettings.DefaultBattlePosition,
    int SoundVolume = GameSettings.DefaultSoundVolume,
    int MusicVolume = GameSettings.DefaultMusicVolume)
{
    public const int CurrentFormatVersion = 6;
    public const int MinVolume = 0;
    public const int MaxVolume = 100;
    public const int DefaultSoundVolume = 50;
    public const int DefaultMusicVolume = 50;
    public const int MinRenderResolution = 50;
    public const int MaxRenderResolution = 100;
    public const int DefaultAutoSaveInterval = 5;
    public const int MinAutoSaveLimit = 1;
    public const int DefaultAutoSaveLimit = 10;
    public const BattleSpeed DefaultBattleSpeed = BattleSpeed.Normal;
    public const MovementSpeed DefaultMovementSpeed = MovementSpeed.Normal;
    public const UiSize DefaultUiSize = UiSize.Normal;
    public const BattlePosition DefaultBattlePosition = BattlePosition.Left;

    public static GameSettings Default { get; } = Create(
        DefaultSoundVolume,
        DefaultMusicVolume,
        false,
        MaxRenderResolution,
        DefaultAutoSaveInterval,
        DefaultAutoSaveLimit,
        DefaultBattleSpeed,
        DefaultUiSize,
        true,
        DefaultMovementSpeed,
        null,
        DefaultBattlePosition);

    [JsonPropertyName("soundVolume2")]
    public int SoundVolume { get; } = Math.Clamp(SoundVolume, MinVolume, MaxVolume);

    [JsonPropertyName("musicVolume2")]
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
        MovementSpeed movementSpeed,
        Language? language,
        BattlePosition battlePosition)
    {
        return new GameSettings(
            CurrentFormatVersion,
            windowed,
            renderResolution,
            autoSaveInterval,
            autoSaveLimit,
            battleSpeed,
            uiSize,
            lockCursor,
            movementSpeed,
            language,
            battlePosition,
            soundVolume,
            musicVolume);
    }
}
