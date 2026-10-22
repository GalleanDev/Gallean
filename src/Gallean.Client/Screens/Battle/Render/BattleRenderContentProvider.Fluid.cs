using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class BattleRenderContentProvider
{
    private static class Fluid
    {
        private const int StillWidth = 288;
        private const int StillHeight = 120;

        public static IReadOnlyList<BattleFluidShaderProgram> BuildShaderPrograms(
            AssetSpriteKey.BattleBackground background,
            IReadOnlyList<BattleFluidConfiguration> configurations,
            IAssetProvider assets)
        {
            using AssetCache<Sprite>.Lease lease = assets.GetRequiredSprite(background);

            Sprite sprite = lease;
            AssetSpriteMetadata metadata = sprite.Metadata;
            SKRect scene = SKRect.Create(metadata.OffsetX, metadata.OffsetY, metadata.Width, metadata.Height);
            SKImage depth = GetLayer(sprite.Depth, background, AssetSpriteLayerType.Depth);
            List<BattleFluidShaderProgram> programs = [];

            foreach (BattleFluidConfiguration configuration in configurations)
            {
                BattleFluidShaderProgram program = new();

                using BattleFluidShaderProgram.Editor editor = program.Edit();

                editor.SetHeightField(BuildSceneShader(depth, scene, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp));
                editor.SetGain((float)configuration.Gain);
                editor.SetExtinction((float)configuration.Extinction);
                editor.SetPowder((float)configuration.Powder);
                editor.SetPowderFloor((float)configuration.PowderFloor);
                editor.SetBodyRamp((float)configuration.BodyRamp);
                editor.SetPeakRamp((float)configuration.PeakRamp);
                editor.SetPeakOffset((float)configuration.PeakOffset);
                editor.SetDeepColor(configuration.DeepColor.ToSKColorF());
                editor.SetBodyColor(configuration.BodyColor.ToSKColorF());
                editor.SetPeakColor(configuration.PeakColor.ToSKColorF());
                editor.SetScatterColor(configuration.ScatterColor.ToSKColorF());

                programs.Add(program);
            }

            return programs;
        }

        public static SKImage BuildStill(
            AssetSpriteKey.BattleBackground background,
            IReadOnlyList<BattleFluidConfiguration> configurations,
            IReadOnlyList<BattleFluidShaderProgram> programs,
            IAssetProvider assets)
        {
            SKImageInfo info = new(StillWidth, StillHeight, SKColorType.RgbaF16, SKAlphaType.Premul);

            using SKSurface surface = SKSurface.Create(info);

            surface.Canvas.Clear(SKColors.Transparent);

            for (int layer = 0; layer < configurations.Count; layer++)
            {
                AssetAnimationKey.BattleBackgroundOverlay overlayKey = new(background.Location, background.Variant, layer);
                using AssetCache<Animation>.Lease? overlay = assets.GetOptionalAnimation(overlayKey);

                if (overlay is not null)
                {
                    DrawStill(surface.Canvas, programs[layer], overlay);
                }
            }

            return surface.Snapshot();
        }

        private static void DrawStill(SKCanvas canvas, BattleFluidShaderProgram program, Animation animation)
        {
            AssetAnimationMetadata metadata = animation.Metadata;
            SKRect scene = SKRect.Create(metadata.OffsetX, metadata.OffsetY, metadata.Width, metadata.Height);

            using SKAutoCanvasRestore restore = new(canvas);

            canvas.Scale(StillWidth / (float)metadata.Width, StillHeight / (float)metadata.Height);
            canvas.Translate(-metadata.OffsetX, -metadata.OffsetY);

            using SKImage thicknessImage = BuildThickness(animation);
            using SKShader thicknessShader = BuildSceneShader(thicknessImage, scene, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
            using BattleFluidShaderProgram.Editor editor = program.Edit();

            editor.SetThickness(thicknessShader);

            using SKShader shader = editor.Build();
            using SKPaint paint = new() { Shader = shader };

            canvas.DrawRect(scene, paint);
        }

        private static SKImage BuildThickness(Animation animation)
        {
            int frameCount = animation.Metadata.FrameCount;
            SKImageInfo info = new(StillWidth, StillHeight, SKColorType.RgbaF16, SKAlphaType.Premul);
            SKRect bounds = SKRect.Create(StillWidth, StillHeight);

            using SKSurface surface = SKSurface.Create(info);
            using SKPaint paint = new() { BlendMode = SKBlendMode.Plus };

            paint.ColorF = new SKColorF(1, 1, 1, 1.0f / frameCount);
            surface.Canvas.Clear(SKColors.Transparent);

            for (int index = 0; index < frameCount; index++)
            {
                surface.Canvas.DrawImage(animation[index].Image, bounds, RenderSamplingOptions.Linear, paint);
            }

            return surface.Snapshot();
        }
    }
}
