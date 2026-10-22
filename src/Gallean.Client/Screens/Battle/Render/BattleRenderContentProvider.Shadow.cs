using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class BattleRenderContentProvider
{
    private static class Shadow
    {
        private const double DynamicStrength = 0.6;

        public static BattleShadowShaderProgram BuildShaderProgram(BattleLightConfiguration configuration, BattleGround ground)
        {
            BattleShadowShaderProgram program = new();

            using BattleShadowShaderProgram.Editor editor = program.Edit();

            editor.SetGroundScale(new SKPoint((float)BattleRenderLayout.GroundDepthPerPixel, (float)BattleRenderLayout.HeightPerPixel));
            editor.SetRiseLimit((float)configuration.ShadowRiseLimit);
            editor.SetPenumbraLimit((float)configuration.ShadowPenumbraLimit);
            editor.SetGroundLight((float)GetGroundLight(configuration, ground));
            editor.SetContrast((float)(configuration.ShadowContrast * DynamicStrength));

            return program;
        }

        public static double GetStrength(BattleLightConfiguration configuration, BattleGround ground)
        {
            return configuration.ShadowContrast * GetSunLight(configuration) / GetGroundLight(configuration, ground);
        }

        private static double GetGroundLight(BattleLightConfiguration configuration, BattleGround ground)
        {
            return configuration.BackgroundAmbient + GetSunLight(configuration) + GetRigLight(configuration, ground);
        }

        private static double GetSunLight(BattleLightConfiguration configuration)
        {
            return GetLambert(BattleRenderLayout.GroundNormal, configuration.SunDirection) * GetLuminance(configuration.SunColor, configuration.SunPower);
        }

        private static double GetRigLight(BattleLightConfiguration configuration, BattleGround ground)
        {
            AnimationPlacement attackers = BattleRenderLayout.GetGridPlacement(BattleSide.Attacker);
            AnimationPlacement defenders = BattleRenderLayout.GetGridPlacement(BattleSide.Defender);
            Point center = attackers.ScaleOrigin + (defenders.ScaleOrigin - attackers.ScaleOrigin) * 0.5;
            Vector3D position = new(center.X, center.Y, -ground.GetDistance(center));
            double result = 0;

            foreach (BattleLight light in configuration.RigLights)
            {
                Vector3D delta = new Vector3D(light.Position.X, light.Position.Y, light.Standoff) - position;
                double relative = delta.Length / light.Range;
                double lambert = GetLambert(BattleRenderLayout.GroundNormal, Vector3D.Normalize(delta));

                result += lambert * GetLuminance(light.Color, light.Power) / (1 + relative * relative);
            }

            return result;
        }

        private static double GetLambert(Vector3D normal, Vector3D direction)
        {
            return Math.Max(Vector3D.Dot(normal, direction), 0) / Math.PI;
        }

        private static double GetLuminance(Color color, double power)
        {
            double red = Math.Pow(color.R / 255.0, 2.2);
            double green = Math.Pow(color.G / 255.0, 2.2);
            double blue = Math.Pow(color.B / 255.0, 2.2);

            return (0.2126 * red + 0.7152 * green + 0.0722 * blue) * power;
        }
    }
}
