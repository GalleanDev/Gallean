using System.Collections.Immutable;
using Gallean.Engine;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Sites
    {
        public readonly record struct Input(
            ImmutableSortedDictionary<WorldSiteId, WorldSite> Sites,
            WorldVisibility Visibility);

        public static IReadOnlyList<MapObject> Build(Input input, IAssetMetadataProvider assets)
        {
            List<MapObject> objects = [];

            foreach (WorldSite site in input.Sites.Values)
            {
                if (IsObjectVisible(input.Visibility, site.Footprint))
                {
                    AssetAnimationKey.SiteType type = GetSiteType(site);

                    objects.Add(new Site(
                        site.Entrance,
                        GetFootprintDepth(site.Footprint, Layer.Sites),
                        site.Footprint,
                        new AssetAnimationKey.Site(type, site.Variant.Value)));

                    AssetAnimationKey.SiteShadow shadowKey = new(type, site.Variant.Value);

                    if (assets.GetOptionalAnimationMetadata(shadowKey) is not null)
                    {
                        objects.Add(new ObjectShadow(
                            site.Entrance,
                            GetFootprintDepth(site.Footprint, Layer.StacksShadow),
                            site.Footprint,
                            shadowKey));
                    }

                    objects.Add(new LocationBanner(
                        site.Entrance,
                        GetDepth(site.Entrance, Layer.Banners),
                        site.Footprint,
                        new AssetAnimationKey.SiteBanner(type)));
                }
            }

            return objects;
        }

        private static AssetAnimationKey.SiteType GetSiteType(WorldSite site)
        {
            return site switch
            {
                WorldMerchantSite => AssetAnimationKey.SiteType.Merchant,
                WorldMageSite => AssetAnimationKey.SiteType.Mage,
                WorldMercenarySite => AssetAnimationKey.SiteType.Mercenary,
                WorldTrainerSite => AssetAnimationKey.SiteType.Trainer,
                _ => throw new ArgumentOutOfRangeException(nameof(site), site, null)
            };
        }
    }
}
