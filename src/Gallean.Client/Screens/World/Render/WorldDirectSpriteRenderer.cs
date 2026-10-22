using SkiaSharp;

namespace Gallean.Client;

internal static class WorldDirectSpriteRenderer
{
    public static void Draw(SKCanvas canvas, SKPaint paint, IAssetProvider assets, WorldRenderContent.DirectSprite directSprite, int frameIndex)
    {
        switch (directSprite)
        {
            case WorldRenderContent.DirectSprite.Animation animation:
                using (AssetCache<Animation>.Lease lease = assets.GetRequiredAnimation(animation.Key))
                {
                    Animation value = lease.Value;
                    Sprite frame = value.FrameByIndex(frameIndex, looping: true);

                    double x = animation.Anchor.X - (value.Metadata.OffsetX + (value.Metadata.Width / 2.0)) + frame.Metadata.OffsetX;
                    double y = animation.Anchor.Y - (value.Metadata.OffsetY + value.Metadata.Height) + frame.Metadata.OffsetY;

                    DrawSprite(canvas, paint, frame, x, y);
                }
                break;

            case WorldRenderContent.DirectSprite.Sprite sprite:
                using (AssetCache<Sprite>.Lease lease = assets.GetRequiredSprite(sprite.Key))
                {
                    Sprite value = lease.Value;

                    DrawSprite(canvas, paint, value, sprite.Anchor.X + value.Metadata.OffsetX, sprite.Anchor.Y + value.Metadata.OffsetY);
                }
                break;
        }
    }

    private static void DrawSprite(SKCanvas canvas, SKPaint paint, Sprite sprite, double x, double y)
    {
        double height = sprite.Image.Height * (sprite.Metadata.Width / (double)sprite.Image.Width);
        SKRect destination = new((float)x, (float)y, (float)(x + sprite.Metadata.Width), (float)(y + height));

        canvas.DrawImage(sprite.Image, destination, RenderSamplingOptions.Linear, paint);
    }
}
