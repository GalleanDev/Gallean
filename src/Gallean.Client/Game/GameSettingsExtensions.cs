using Gallean.Configuration;

namespace Gallean.Client;

internal static class GameSettingsExtensions
{
    extension(GameSettings settings)
    {
        public float SoundVolumeRatio => settings.SoundVolume / (float)GameSettings.MaxVolume;
        public float MusicVolumeRatio => settings.MusicVolume / (float)GameSettings.MaxVolume;
        public float RenderResolutionRatio => settings.RenderResolution / (float)GameSettings.MaxRenderResolution;
        public double? UiScaleRatio => settings.UiScale / 100.0;

        public double BattleSpeedFactor => settings.BattleSpeed switch
        {
            BattleSpeed.Normal => 1.2,
            BattleSpeed.Fast => 1.5
        };
    }
}
