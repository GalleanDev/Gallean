using Avalonia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class BattleRenderContentProvider
{
    private static class Ground
    {
        private const int Columns = 360;

        public static BattleGround Build(AssetSpriteKey.BattleBackground background, IAssetProvider assets)
        {
            using AssetCache<Sprite>.Lease lease = assets.GetRequiredSprite(background);

            Sprite sprite = lease;
            SKImage depth = GetLayer(sprite.Depth, background, AssetSpriteLayerType.Depth);
            AssetSpriteMetadata metadata = sprite.Metadata;
            Rect bounds = new(metadata.OffsetX, metadata.OffsetY, metadata.Width, metadata.Height);
            int rows = Math.Max(1, Columns * depth.Height / depth.Width);

            BattleGround relative = new(bounds, Columns, rows, ReadDepths(depth, Columns, rows), DepthScale: 1);

            (Point far, Point near) = GetAnchors();
            double sceneDepth = (near.Y - far.Y) * BattleRenderLayout.SceneDepthPerPixel;
            double relativeDepth = relative.GetDistance(far) - relative.GetDistance(near);

            return relative with { DepthScale = sceneDepth / relativeDepth };
        }

        private static float[] ReadDepths(SKImage depth, int columns, int rows)
        {
            SKImageInfo info = new(columns, rows, SKColorType.Rgba8888, SKAlphaType.Premul);

            using SKSurface surface = SKSurface.Create(info);
            surface.Canvas.DrawImage(depth, SKRect.Create(columns, rows), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            using SKPixmap pixmap = surface.PeekPixels();

            ReadOnlySpan<byte> pixels = pixmap.GetPixelSpan();
            float[] depths = new float[columns * rows];

            for (int index = 0; index < depths.Length; index++)
            {
                depths[index] = pixels[index * 4] / 255.0f;
            }

            return depths;
        }

        private static (Point Far, Point Near) GetAnchors()
        {
            List<Point> points = [];

            foreach (BattleSide side in Enum.GetValues<BattleSide>())
            {
                foreach (GridPosition position in GridPosition.All)
                {
                    points.Add(BattleRenderLayout.GetFootPoint(new BattleGridFootprint(side, position, false)));
                }
            }

            return (points.MinBy(foot => foot.Y), points.MaxBy(foot => foot.Y));
        }
    }
}
