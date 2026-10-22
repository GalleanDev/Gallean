using GameContent;

namespace Packer;

internal static class PackBuilderCatalog
{
    public static IReadOnlyList<PackBuilderEntry> ListEntries(IReadOnlyList<GameAssetDescriptor> descriptors)
    {
        Dictionary<AssetKey, GameAssetDescriptor.Sound> sounds = [];
        Dictionary<AssetKey, GameAssetDescriptor.Animation> animations = [];

        foreach (GameAssetDescriptor descriptor in descriptors)
        {
            switch (descriptor)
            {
                case GameAssetDescriptor.Sound sound:
                    sounds.Add(sound.Key, sound);
                    break;

                case GameAssetDescriptor.Animation animation:
                    animations.Add(animation.Key, animation);
                    break;
            }
        }

        List<PackBuilderEntry> entries = [];

        foreach (GameAssetDescriptor descriptor in descriptors)
        {
            PackBuilderEntry entry = descriptor switch
            {
                GameAssetDescriptor.Sprite sprite => new PackBuilderEntry.GameAsset(sprite),
                GameAssetDescriptor.Animation animation => GetAnimationEntry(animation, animations),
                GameAssetDescriptor.Sound sound => GetSoundEntry(sound, sounds),
                _ => throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor, null)
            };

            entries.Add(entry);
        }

        foreach (AssetSpriteKey.BattleBackground background in AssetSpriteKey.BattleBackground.ListAll())
        {
            entries.Add(new PackBuilderEntry.Background(background, BackgroundBuilder.BattleSceneWidth));

            IReadOnlyList<UmbraFluidConfiguration> fluids = FluidMap.GetConfigurations(background);

            for (int layer = 0; layer < fluids.Count; layer++)
            {
                entries.Add(new PackBuilderEntry.BackgroundOverlay(background, layer));
            }
        }

        entries.Add(new PackBuilderEntry.Background(new AssetSpriteKey.MainMenuBackgroundV2(), BackgroundBuilder.MainMenuSceneWidth));

        return entries;
    }

    private static PackBuilderEntry GetAnimationEntry(
        GameAssetDescriptor.Animation descriptor,
        IReadOnlyDictionary<AssetKey, GameAssetDescriptor.Animation> animations)
    {
        AssetKey? recordKey = SoundRecordMap.GetRecordKey(descriptor.Key);

        if (recordKey is not null)
        {
            return new PackBuilderEntry.UnitAttack(descriptor, animations[recordKey]);
        }
        else
        {
            return new PackBuilderEntry.GameAsset(descriptor);
        }
    }

    private static PackBuilderEntry GetSoundEntry(
        GameAssetDescriptor.Sound descriptor,
        IReadOnlyDictionary<AssetKey, GameAssetDescriptor.Sound> sounds)
    {
        AssetKey? recordKey = SoundRecordMap.GetRecordKey(descriptor.Key);

        if (recordKey is not null)
        {
            return new PackBuilderEntry.UnitSound(descriptor, sounds[recordKey]);
        }
        else
        {
            return new PackBuilderEntry.GameAsset(descriptor);
        }
    }
}
