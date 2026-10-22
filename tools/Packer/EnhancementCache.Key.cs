using Gallean.Common;

namespace Packer;

internal sealed partial class EnhancementCache
{
    public readonly record struct Key(
        IReadOnlyList<EnhancementType> Enhancements,
        UpscalerType? Upscaler,
        UmbraSpriteHash Hash,
        AssetSpriteLayerType? LayerType,
        UmbraPromptHash? PromptHash)
    {
        public bool Equals(Key other)
        {
            return Enhancements.SequenceEqual(other.Enhancements) &&
                   Upscaler == other.Upscaler &&
                   Hash == other.Hash &&
                   LayerType == other.LayerType &&
                   PromptHash == other.PromptHash;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Enhancements);
            hash.Add(Upscaler);
            hash.Add(Hash);
            hash.Add(LayerType);
            hash.Add(PromptHash);

            return hash.ToHashCode();
        }
    }
}
