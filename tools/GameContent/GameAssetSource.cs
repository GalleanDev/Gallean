using Gallean.Entities.GameData;

namespace GameContent;

public sealed class GameAssetSource : IAssetSource<ReadOnlyMemory<byte>>, IGameAssetReader
{
    private readonly GameSourceCache cache;
    private readonly IReadOnlyDictionary<AssetKey, GameAssetDescriptor> descriptorsByKey;

    private GameAssetSource()
    {
        cache = new GameSourceCache();

        GameFiles gameFiles = GameFiles.Original;

        GameImages animations = GameImages.Load(gameFiles.ImagesRoot, "IsoAnim.ff", cache);
        GameImages battleImages = GameImages.Load(gameFiles.ImagesRoot, "Battle.ff", cache);
        GameImages battleUnits = GameImages.Load(gameFiles.ImagesRoot, "BatUnits.ff", cache);
        GameImages battleItems = GameImages.Load(gameFiles.ImagesRoot, "BatItems.ff", cache);
        GameImages capitalImages = GameImages.Load(gameFiles.ImagesRoot, "Capital.ff", cache);
        GameImages cityImages = GameImages.Load(gameFiles.ImagesRoot, "City.ff", cache);
        GameImages common = GameImages.Load(gameFiles.ImagesRoot, "IsoCmon.ff", cache);
        GameImages cursors = GameImages.Load(gameFiles.ImagesRoot, "IsoCursr.ff", cache);
        GameImages events = GameImages.Load(gameFiles.ImagesRoot, "Events.ff", cache);
        GameImages faces = GameImages.Load(gameFiles.ImagesRoot, "Faces.ff", cache);
        GameImages ground = GameImages.Load(gameFiles.ImagesRoot, "Ground.ff", cache);
        GameImages groundBorders = GameImages.Load(gameFiles.ImagesRoot, "GrBorder.ff", cache);
        GameImages iconBuildings = GameImages.Load(gameFiles.ImagesRoot, "IconBld.ff", cache);
        GameImages iconItems = GameImages.Load(gameFiles.ImagesRoot, "IconItem.ff", cache);
        GameImages iconSpells = GameImages.Load(gameFiles.ImagesRoot, "IconSpel.ff", cache);
        GameImages lordFaces = GameImages.Load(gameFiles.ImagesRoot, "Lords.ff", cache);
        GameImages spellImages = GameImages.Load(gameFiles.ImagesRoot, "IsoSpell.ff", cache);
        GameImages still = GameImages.Load(gameFiles.ImagesRoot, "IsoStill.ff", cache);
        GameImages thiefActions = GameImages.Load(gameFiles.ImagesRoot, "Thief.ff", cache);
        GameImages terrainObjects = GameImages.Load(gameFiles.ImagesRoot, "IsoTerrn.ff", cache);
        GameImages units = GameImages.Load(gameFiles.ImagesRoot, "IsoUnit.ff", cache);
        GameImages interfaceImages = GameImages.Load(gameFiles.InterfaceRoot, "Interf.ff", cache);

        GlobalDatabase globalDatabase = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        ScenarioDatabase scenarioDatabase = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        CampaignDatabase campaignDatabase = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        GameDatabase database = new(globalDatabase, campaignDatabase);
        GameGraphicsResolver graphicsResolver = new(database, units, battleUnits);
        GameMusicSource? musicSource = GameMusicSource.TryLoad(gameFiles.MusicRoot);
        GameMusicSource? briefingVoiceSource = GameMusicSource.TryLoad(gameFiles.BriefingRoot);
        GameUnitSoundsSource? unitSoundsSource = GameUnitSoundsSource.TryLoad(gameFiles.SoundsRoot, cache);

        Terrain = new GameTerrainSource(ground, groundBorders, terrainObjects, animations, common);
        WorldObjects = new GameWorldObjectSource(still, common, animations);
        SpellEffects = new GameSpellEffectSource(spellImages);
        Stacks = new GameStackSource(units, common, graphicsResolver);
        Battle = new GameBattleSource(battleImages, battleUnits, battleItems, graphicsResolver);
        Cursors = new GameCursorSource(cursors, battleImages);
        Capitals = new GameCapitalSource(capitalImages, CapitalSceneExtractor.Load(Path.Combine(gameFiles.ImagesRoot, "capital.dat")));
        Cities = new GameCitySource(cityImages);
        Portraits = new GamePortraitSource(faces, events, lordFaces, thiefActions, graphicsResolver);
        Icons = new GameIconSource(iconSpells, iconBuildings, iconItems);
        Interfaces = new GameInterfaceSource(interfaceImages, SceneInterfaceExtractor.Load(Path.Combine(gameFiles.InterfaceRoot, "Interf.dlg")));
        Audio = new GameAudioSource(musicSource, briefingVoiceSource, unitSoundsSource, database);

        Descriptors = GameAssetCatalog.ListDescriptors(GameAssetInputs.Load(globalDatabase, scenarioDatabase));

        Dictionary<AssetKey, GameAssetDescriptor> descriptors = [];
        foreach (GameAssetDescriptor descriptor in Descriptors)
        {
            descriptors.Add(descriptor.Key, descriptor);
        }
        descriptorsByKey = descriptors;
    }

    public IReadOnlyList<GameAssetDescriptor> Descriptors { get; }

    internal GameAudioSource Audio { get; }
    internal GameBattleSource Battle { get; }
    internal GameCapitalSource Capitals { get; }
    internal GameCitySource Cities { get; }
    internal GameCursorSource Cursors { get; }
    internal GameIconSource Icons { get; }
    internal GameInterfaceSource Interfaces { get; }
    internal GamePortraitSource Portraits { get; }
    internal GameSpellEffectSource SpellEffects { get; }
    internal GameStackSource Stacks { get; }
    internal GameTerrainSource Terrain { get; }
    internal GameWorldObjectSource WorldObjects { get; }

    public AssetSprite? TryGetSprite(AssetSpriteKey key)
    {
        return cache.GetOrAddSprite(key, BuildSprite);
    }

    public AssetSpriteMetadata? TryGetSpriteMetadata(AssetSpriteKey key)
    {
        return TryGetSprite(key)?.Metadata;
    }

    public AssetSpriteLayer? TryGetSpriteLayer(AssetSpriteKey key, AssetSpriteLayerType type)
    {
        return null;
    }

    public AssetAnimation? TryGetAnimation(AssetAnimationKey key)
    {
        return cache.GetOrAddAnimation(key, BuildAnimation);
    }

    public AssetAnimationMetadata? TryGetAnimationMetadata(AssetAnimationKey key)
    {
        return TryGetAnimation(key)?.Metadata;
    }

    public IReadOnlyList<AssetSpriteLayer>? TryGetAnimationLayer(AssetAnimationKey key, AssetSpriteLayerType type)
    {
        return null;
    }

    public AssetSound? TryGetSound(AssetSoundKey key)
    {
        return cache.GetOrAddSound(key, BuildSound);
    }

    public AssetSoundMetadata? TryGetSoundMetadata(AssetSoundKey key)
    {
        return TryGetSound(key)?.Metadata;
    }

    public AssetStream? TryGetStream(AssetKey key)
    {
        if (key is AssetSoundKey soundKey)
        {
            return TryGetSound(soundKey)?.Data;
        }
        else
        {
            return null;
        }
    }

    public void Dispose()
    {
    }

    public static GameAssetSource? TryLoadDefault()
    {
        if (!Directory.Exists(GameFiles.Original.Root))
        {
            return null;
        }

        return new GameAssetSource();
    }

    AssetSprite? IGameAssetReader.Read(GameAssetDescriptor.Sprite descriptor)
    {
        return descriptor.Extract(this);
    }

    AssetAnimation? IGameAssetReader.Read(GameAssetDescriptor.Animation descriptor)
    {
        return descriptor.Extract(this);
    }

    AssetSound? IGameAssetReader.Read(GameAssetDescriptor.Sound descriptor)
    {
        return descriptor.Extract(this);
    }

    private AssetSprite? BuildSprite(AssetSpriteKey key)
    {
        if (descriptorsByKey.TryGetValue(key, out GameAssetDescriptor? descriptor) &&
            descriptor is GameAssetDescriptor.Sprite sprite)
        {
            return sprite.Extract(this);
        }
        else
        {
            return null;
        }
    }

    private AssetAnimation? BuildAnimation(AssetAnimationKey key)
    {
        if (descriptorsByKey.TryGetValue(key, out GameAssetDescriptor? descriptor) &&
            descriptor is GameAssetDescriptor.Animation animation)
        {
            return animation.Extract(this);
        }
        else
        {
            return null;
        }
    }

    private AssetSound? BuildSound(AssetSoundKey key)
    {
        if (descriptorsByKey.TryGetValue(key, out GameAssetDescriptor? descriptor) &&
            descriptor is GameAssetDescriptor.Sound sound)
        {
            return sound.Extract(this);
        }
        else
        {
            return null;
        }
    }
}
