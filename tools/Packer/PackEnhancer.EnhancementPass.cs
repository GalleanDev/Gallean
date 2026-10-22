namespace Packer;

internal sealed partial class PackEnhancer
{
    private sealed record EnhancementPass(
        EnhancementType Enhancement,
        IReadOnlyList<UmbraSurfaceLayerType> LayerTypes)
    {
        public static readonly EnhancementPass Upscale = new(EnhancementType.Upscale, []);
        public static readonly EnhancementPass Refine = new(EnhancementType.Refine, []);
        public static readonly EnhancementPass Alpha = new(EnhancementType.Alpha, []);

        public static EnhancementPass Surface(IReadOnlyList<UmbraSurfaceLayerType> layerTypes)
        {
            return new EnhancementPass(EnhancementType.Surface, layerTypes);
        }

        public List<EnhancementCache.Key> GetOutputKeys(HashedEntry entry)
        {
            IReadOnlyList<EnhancementType> enhancements = EnhancementMap.GetOutputEnhancements(entry.Enhancements, Enhancement);
            UmbraPromptHash? promptHash = enhancements.Contains(EnhancementType.Refine) ? entry.PromptHash : null;
            List<EnhancementCache.Key> keys = [];

            foreach (UmbraSpriteHash hash in entry.FrameHashes)
            {
                if (LayerTypes.Count > 0)
                {
                    foreach (UmbraSurfaceLayerType type in LayerTypes)
                    {
                        keys.Add(new EnhancementCache.Key(enhancements, entry.Upscaler, hash, type.FromUmbra(), promptHash));
                    }
                }
                else
                {
                    keys.Add(new EnhancementCache.Key(enhancements, entry.Upscaler, hash, null, promptHash));
                }
            }

            return keys;
        }
    }
}
