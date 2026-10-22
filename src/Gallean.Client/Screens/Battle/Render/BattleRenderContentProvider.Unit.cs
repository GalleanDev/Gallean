using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class BattleRenderContentProvider
{
    private static class Unit
    {
        private const int AmbientWidth = 64;
        private const int AmbientHeight = 32;
        private const float AmbientBlur = 5.0f;
        private const int ReflectionWidth = 256;
        private const int ReflectionHeight = 128;
        private const float ReflectionBlur = 1.5f;

        public static BattleUnitShaderProgram BuildShaderProgram(
            AssetSpriteKey.BattleBackground background,
            SKImage fluidStill,
            IAssetProvider assets)
        {
            using AssetCache<Sprite>.Lease lease = assets.GetRequiredSprite(background);

            Sprite sprite = lease;

            BattleUnitShaderProgram program = new();

            using BattleUnitShaderProgram.Editor editor = program.Edit();

            editor.SetAmbient(BuildMap(sprite.Image, fluidStill, AmbientWidth, AmbientHeight, AmbientBlur));
            editor.SetAmbientSize(new SKPoint(AmbientWidth, AmbientHeight));
            editor.SetReflection(BuildMap(sprite.Image, fluidStill, ReflectionWidth, ReflectionHeight, ReflectionBlur));
            editor.SetReflectionSize(new SKPoint(ReflectionWidth, ReflectionHeight));

            return program;
        }

        private static SKShader BuildMap(SKImage background, SKImage fluidStill, int width, int height, float blur)
        {
            SKImageInfo info = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            SKRect bounds = SKRect.Create(width, height);

            using SKSurface reduceSurface = SKSurface.Create(info);
            reduceSurface.Canvas.DrawImage(background, bounds, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            reduceSurface.Canvas.DrawImage(fluidStill, bounds, RenderSamplingOptions.Linear);

            using SKImage reduceImage = reduceSurface.Snapshot();

            using SKSurface blurSurface = SKSurface.Create(info);
            using SKImageFilter blurFilter = SKImageFilter.CreateBlur(blur, blur, SKShaderTileMode.Clamp);
            using SKPaint blurPaint = new() { ImageFilter = blurFilter };
            blurSurface.Canvas.DrawImage(reduceImage, bounds, RenderSamplingOptions.Linear, blurPaint);
            using SKImage blurImage = blurSurface.CreateRasterImage();

            return blurImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear);
        }
    }
}
