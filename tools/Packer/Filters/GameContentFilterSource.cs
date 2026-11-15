using GameContent;

namespace Packer;

internal sealed class GameContentFilterSource(IReadOnlyList<PackBuilderEntry> entries, IGameAssetReader reader) : ISpriteFilterSource
{
    private readonly IReadOnlyDictionary<AssetKey, GameAssetDescriptor> descriptors = GetDescriptors(entries);

    public IReadOnlyList<AssetSprite>? ReadFrames(AssetKey key)
    {
        if (!descriptors.TryGetValue(key, out GameAssetDescriptor? descriptor))
        {
            return null;
        }

        return descriptor switch
        {
            GameAssetDescriptor.Sprite sprite => ReadSpriteFrames(sprite),
            GameAssetDescriptor.Animation animation => ReadAnimationFrames(animation),
            GameAssetDescriptor.Sound => null
        };
    }

    private IReadOnlyList<AssetSprite>? ReadSpriteFrames(GameAssetDescriptor.Sprite descriptor)
    {
        AssetSprite? sprite = reader.Read(descriptor);

        if (sprite is not null)
        {
            return [sprite];
        }
        else
        {
            return null;
        }
    }

    private IReadOnlyList<AssetSprite>? ReadAnimationFrames(GameAssetDescriptor.Animation descriptor)
    {
        AssetAnimation? animation = reader.Read(descriptor);

        return animation?.Frames;
    }

    private static IReadOnlyDictionary<AssetKey, GameAssetDescriptor> GetDescriptors(IReadOnlyList<PackBuilderEntry> entries)
    {
        Dictionary<AssetKey, GameAssetDescriptor> descriptors = [];

        foreach (PackBuilderEntry entry in entries)
        {
            switch (entry)
            {
                case PackBuilderEntry.GameAsset asset:
                    descriptors.Add(asset.Key, asset.Descriptor);
                    break;

                case PackBuilderEntry.UnitAttack attack:
                    descriptors.Add(attack.Key, attack.Descriptor);
                    break;
            }
        }

        return descriptors;
    }
}
