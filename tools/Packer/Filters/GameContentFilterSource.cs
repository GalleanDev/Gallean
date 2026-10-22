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

        switch (descriptor)
        {
            case GameAssetDescriptor.Sprite spriteDescriptor:
                AssetSprite? sprite = reader.Read(spriteDescriptor);

                if (sprite is not null)
                {
                    return [sprite];
                }
                else
                {
                    return null;
                }

            case GameAssetDescriptor.Animation animationDescriptor:
                AssetAnimation? animation = reader.Read(animationDescriptor);
                return animation?.Frames;

            default:
                throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }
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
