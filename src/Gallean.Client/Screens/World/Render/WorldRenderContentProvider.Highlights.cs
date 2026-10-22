using Avalonia;
using Gallean.Common;
using static Gallean.Client.WorldRenderContent;
using static Gallean.Client.WorldRenderLayout;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    private static class Highlights
    {
        public static IReadOnlyList<HighlightLayer> Build(HighlightOverlay overlay, IAssetProvider assets)
        {
            using CleanupList leases = new();

            List<HighlightLayer> layers = [];

            foreach (Highlight highlight in overlay.Items)
            {
                IReadOnlyList<DirectSprite> sprites = BuildSprites(highlight.Target, assets, leases);

                Rect bounds = sprites[0].Bounds;

                foreach (DirectSprite sprite in sprites)
                {
                    bounds = bounds.Union(sprite.Bounds);
                }

                layers.Add(new HighlightLayer(sprites, highlight.Color, bounds));
            }

            return layers;
        }

        private static IReadOnlyList<DirectSprite> BuildSprites(MapObject target, IAssetProvider assets, CleanupList leases)
        {
            switch (target)
            {
                case Stack stack:
                    Animation stackAnimation = leases.Add(assets.GetRequiredAnimation(stack.AssetKey)).Value;
                    Sprite bannerSprite = leases.Add(assets.GetRequiredSprite(stack.BannerKey)).Value;
                    Point center = WorldTransform.ToWorldPoint(stack.Position);

                    return
                    [
                        new DirectSprite.Animation(
                            stack.AssetKey,
                            Tile.GetAnchor(stack.Position, stackAnimation.Metadata),
                            stack.Depth,
                            GetFrameBounds(center, stackAnimation.Metadata),
                            Opacity: 1.0),
                        new DirectSprite.Sprite(
                            stack.BannerKey,
                            center,
                            stack.Depth,
                            GetSpriteBounds(center, bannerSprite),
                            Opacity: 1.0)
                    ];

                case CityShield cityShield:
                    Sprite shieldSprite = leases.Add(assets.GetRequiredSprite(cityShield.AssetKey)).Value;
                    Point shieldCenter = Footprint.GetAnchor(cityShield.Footprint);

                    return
                    [
                        new DirectSprite.Sprite(
                            cityShield.AssetKey,
                            shieldCenter,
                            cityShield.Depth,
                            GetSpriteBounds(shieldCenter, shieldSprite),
                            Opacity: 1.0)
                    ];

                case Fortification fortification:
                    Animation fortificationAnimation = leases.Add(assets.GetRequiredAnimation(fortification.AssetKey)).Value;
                    Point origin = Footprint.GetAnchor(fortification.Footprint);
                    Point anchor = Footprint.GetAnchor(fortification.Footprint, fortificationAnimation.Metadata);

                    return
                    [
                        new DirectSprite.Animation(
                            fortification.AssetKey,
                            anchor,
                            fortification.Depth,
                            GetFrameBounds(origin, fortificationAnimation.Metadata),
                            Opacity: 1.0)
                    ];

                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, "Only stacks, city shields and fortifications carry a highlight.");
            }
        }
    }
}
