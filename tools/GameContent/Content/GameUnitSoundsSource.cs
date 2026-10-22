namespace GameContent;

internal sealed class GameUnitSoundsSource
{
    private const int MissingRecordFrameIndex = 1;

    private readonly IReadOnlyDictionary<string, UnitTypeSounds> unitSounds;
    private readonly IReadOnlyList<SoundsExtractor> banks;
    private readonly GameSourceCache cache;

    private GameUnitSoundsSource(
        IReadOnlyDictionary<string, UnitTypeSounds> unitSounds,
        IReadOnlyList<SoundsExtractor> banks,
        GameSourceCache cache)
    {
        this.unitSounds = unitSounds;
        this.banks = banks;
        this.cache = cache;
    }

    public static GameUnitSoundsSource? TryLoad(string soundsRoot, GameSourceCache cache)
    {
        if (!Directory.Exists(soundsRoot))
        {
            return null;
        }

        List<SoundsExtractor> banks = [];

        foreach (string path in Directory.EnumerateFiles(soundsRoot, "*.wdb"))
        {
            banks.Add(SoundsExtractor.Load(path));
        }

        Dictionary<string, UnitTypeSounds> unitSounds = new(StringComparer.OrdinalIgnoreCase);

        foreach (string path in Directory.EnumerateFiles(soundsRoot, "*.wdt"))
        {
            SoundsMappingExtractor mapping = SoundsMappingExtractor.Load(path);

            foreach ((string unitId, UnitTypeSounds sounds) in mapping.GetAllUnitTypeSounds())
            {
                unitSounds[unitId] = sounds;
            }
        }

        if (banks.Count <= 0 || unitSounds.Count <= 0)
        {
            return null;
        }

        return new GameUnitSoundsSource(unitSounds, banks, cache);
    }

    public bool HasUnitSounds(string unitId, UnitSoundType type)
    {
        if (!unitSounds.TryGetValue(unitId, out UnitTypeSounds? sounds))
        {
            return false;
        }

        return GetSoundNames(sounds, type).Count > 0;
    }

    public IReadOnlyList<AssetSound>? GetUnitSounds(string unitId, UnitSoundType type)
    {
        if (!unitSounds.TryGetValue(unitId, out UnitTypeSounds? sounds))
        {
            return null;
        }

        IReadOnlyList<AssetSound>? clips = GetSounds(GetSoundNames(sounds, type));

        if (clips is null)
        {
            return null;
        }

        int? frameIndex = GetAnimationFrameIndex(sounds, type);

        if (frameIndex is null)
        {
            return clips;
        }

        int animationFrame = frameIndex.Value;
        List<AssetSound> timed = [];

        foreach (AssetSound clip in clips)
        {
            timed.Add(clip with { Metadata = new AssetSoundMetadata(animationFrame, StartTime: null) });
        }

        return timed;
    }

    public GameUnitAttackTriggers GetAttackTriggers(string unitId)
    {
        if (unitSounds.TryGetValue(unitId, out UnitTypeSounds? sounds))
        {
            return GetAttackTriggers(sounds);
        }
        else
        {
            return new GameUnitAttackTriggers(MissingRecordFrameIndex, MissingRecordFrameIndex, MissingRecordFrameIndex);
        }
    }

    public IReadOnlyList<AssetSound>? GetSounds(IReadOnlyList<string> names)
    {
        List<AssetSound> variants = [];

        foreach (string name in names)
        {
            AssetSound? clip = cache.GetOrAddSoundClip(name, LoadClip);

            if (clip is not null)
            {
                variants.Add(clip);
            }
        }

        if (variants.Count > 0)
        {
            return variants;
        }
        else
        {
            return null;
        }
    }

    private AssetSound? LoadClip(string name)
    {
        foreach (SoundsExtractor bank in banks)
        {
            byte[]? sound = bank.GetSound(name);

            if (sound is not null)
            {
                return new AssetSound(new AssetSoundMetadata(), AssetStream.FromBytes(sound));
            }
        }

        return null;
    }

    internal static IReadOnlyList<string> GetSoundNames(UnitTypeSounds sounds, UnitSoundType type)
    {
        return type switch
        {
            UnitSoundType.Attack => sounds.AttackSounds,
            UnitSoundType.Hit => sounds.HitSounds,
            UnitSoundType.Touch => sounds.TouchSounds,
            UnitSoundType.Damaged => sounds.DamagedSounds,
            UnitSoundType.Walk => sounds.WalkSounds
        };
    }

    internal static int? GetAnimationFrameIndex(UnitTypeSounds sounds, UnitSoundType type)
    {
        return type switch
        {
            UnitSoundType.Attack => sounds.BeginAttackSoundFrameIndex,
            UnitSoundType.Hit => null,
            UnitSoundType.Touch => null,
            UnitSoundType.Damaged => null,
            UnitSoundType.Walk => null
        };
    }

    internal static GameUnitAttackTriggers GetAttackTriggers(UnitTypeSounds sounds)
    {
        return new GameUnitAttackTriggers(
            sounds.EndAttackSoundFrameIndex,
            sounds.BeginAttackHitSoundFrameIndex,
            sounds.EndAttackHitSoundFrameIndex);
    }
}
