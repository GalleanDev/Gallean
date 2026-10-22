using Avalonia;

namespace Gallean.Client;

internal static class WorldRenderLayout
{
    public static class Tile
    {
        public static Point GetAnchor(WorldPosition position, AssetAnimationMetadata animation)
        {
            return WorldTransform.ToWorldPoint(position) + new Vector(
                animation.OffsetX + animation.Width / 2.0,
                animation.OffsetY + animation.Height);
        }
    }

    public static class Footprint
    {
        public static Point GetAnchor(WorldFootprint footprint)
        {
            Point topLeft = WorldTransform.ToWorldPoint(footprint.TopLeft);
            Point bottomRight = WorldTransform.ToWorldPoint(new WorldPosition(
                footprint.TopLeft.X + footprint.Width - 1,
                footprint.TopLeft.Y + footprint.Height - 1));

            return new Point((topLeft.X + bottomRight.X) / 2.0, (topLeft.Y + bottomRight.Y) / 2.0);
        }

        public static Point GetAnchor(WorldFootprint footprint, AssetAnimationMetadata animation)
        {
            return GetAnchor(footprint) + new Vector(
                animation.OffsetX + animation.Width / 2.0,
                animation.OffsetY + animation.Height);
        }
    }
}
