using Gallean.Common;

namespace Gallean.Client;

internal sealed class Animation(AssetAnimationMetadata metadata, IReadOnlyList<Sprite> frames, ImmutableBitmap? hitMask)
{
    public AssetAnimationMetadata Metadata { get; } = metadata;

    public ImmutableBitmap? HitMask { get; } = hitMask;

    public Sprite this[int index] => frames[index];
}
