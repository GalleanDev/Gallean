namespace Gallean.Client;

internal sealed class Animation(AssetAnimationMetadata metadata, IReadOnlyList<Sprite> frames)
{
    public AssetAnimationMetadata Metadata { get; } = metadata;

    public Sprite this[int index] => frames[index];
}
