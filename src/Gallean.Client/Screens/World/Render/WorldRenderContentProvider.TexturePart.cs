namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    private sealed class TexturePart
    {
        private IReadOnlySet<AtlasTexture.RegionKey>? cachedRegions;
        private AtlasTexture? cachedTexture;

        public AtlasTexture Resolve(
            IReadOnlySet<AtlasTexture.RegionKey> regions,
            Func<IReadOnlySet<AtlasTexture.RegionKey>, AtlasTexture> buildFunc)
        {
            if (cachedTexture is not null && ReferenceEquals(cachedRegions, regions))
            {
                return cachedTexture;
            }

            if (cachedTexture is not null && ContainsAllRegions(cachedTexture, regions))
            {
                cachedRegions = regions;

                return cachedTexture;
            }

            AtlasTexture texture = buildFunc(regions);

            cachedRegions = regions;
            cachedTexture = texture;

            return texture;
        }

        private static bool ContainsAllRegions(AtlasTexture texture, IReadOnlySet<AtlasTexture.RegionKey> regions)
        {
            foreach (AtlasTexture.RegionKey region in regions)
            {
                if (!texture.Regions.ContainsKey(region))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
