using System.Globalization;
using Gallean.Configuration;

namespace Gallean.Client;

internal static class GameSettingsExtensions
{
    extension(GameSettings settings)
    {
        public float SoundVolumeRatio => settings.SoundVolume / (float)GameSettings.MaxVolume;
        public float MusicVolumeRatio => settings.MusicVolume / (float)GameSettings.MaxVolume;
        public float RenderResolutionRatio => settings.RenderResolution / (float)GameSettings.MaxRenderResolution;

        public Language EffectiveLanguage => settings.Language ?? Language.FromCulture(CultureInfo.CurrentUICulture);

        public double BattleSpeedFactor => settings.BattleSpeed switch
        {
            BattleSpeed.Normal => 1.2,
            BattleSpeed.Fast => 1.5,
            BattleSpeed.VeryFast => 2.4
        };

        public double MovementSpeedFactor => settings.MovementSpeed switch
        {
            MovementSpeed.Normal => 1.0,
            MovementSpeed.Fast => 1.3,
            MovementSpeed.VeryFast => 2.0
        };

        public double UiSizeFactor => settings.UiSize switch
        {
            UiSize.Small => 0.85,
            UiSize.Normal => 1.0,
            UiSize.Large => 1.2
        };
    }
}
