using Gallean.Common;
using Gallean.Engine.Contracts;
using static GameContent.GameAssetDescriptor;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed partial class GameAudioSource(
    GameMusicSource? music,
    GameMusicSource? briefingVoices,
    GameUnitSoundsSource? sounds,
    GameDatabase database)
{
    public static IEnumerable<GameAssetDescriptor> ListDescriptors(
        IReadOnlyCollection<UnitDefinitionId> unitIds,
        IReadOnlyCollection<WorldSpellDefinitionId> spellIds,
        IReadOnlyCollection<ScenarioId> scenarioIds)
    {
        yield return OptionalSound(
            new AssetSoundKey.StackDestroy(),
            source => source.Audio.GetBankSound(Audio.StackDestroySounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.StackSummonDestroy(),
            source => source.Audio.GetBankSound(Audio.StackSummonDestroySounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.StackCreate(),
            source => source.Audio.GetBankSound(Audio.StackCreateSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.StackBoatMove(),
            source => source.Audio.GetBankSound(Audio.StackBoatMoveSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.InterfaceOpen(),
            source => source.Audio.GetBankSound(Audio.InterfaceOpenSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.Beep(),
            source => source.Audio.GetBankSound(Audio.BeepSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.BookOpen(),
            source => source.Audio.GetBankSound(Audio.BookOpenSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.BookClose(),
            source => source.Audio.GetBankSound(Audio.BookCloseSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.TurnStart(),
            source => source.Audio.GetBankSound(Audio.TurnStartSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.WorldBattleStart(),
            source => source.Audio.GetBankSound(Audio.WorldBattleStartSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.Occupy(),
            source => source.Audio.GetBankSound(Audio.OccupySounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.RuinEnter(),
            source => source.Audio.GetBankSound(Audio.RuinEnterSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.CityEnter(),
            source => source.Audio.GetBankSound(Audio.CityEnterSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.CityExit(),
            source => source.Audio.GetBankSound(Audio.CityExitSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.RuinLoot(),
            source => source.Audio.GetBankSound(Audio.RuinLootSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.BuildingConstruct(),
            source => source.Audio.GetBankSound(Audio.BuildingConstructSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.CityGrow(),
            source => source.Audio.GetBankSound(Audio.CityGrowSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.CityRiotEnd(),
            source => source.Audio.GetBankSound(Audio.CityRiotEndSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.BagPickUp(),
            source => source.Audio.GetBankSound(Audio.BagPickUpSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.UnitPurchase(),
            source => source.Audio.GetBankSound(Audio.UnitPurchaseSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.SitePurchase(),
            source => source.Audio.GetBankSound(Audio.SitePurchaseSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.SiteEnter(),
            source => source.Audio.GetBankSound(Audio.SiteEnterSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.ItemUse(),
            source => source.Audio.GetBankSound(Audio.ItemUseSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.SpellResearch(),
            source => source.Audio.GetBankSound(Audio.SpellResearchSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.ThiefAction(),
            source => source.Audio.GetBankSound(Audio.ThiefActionSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.MenuMusic(),
            source => source.Audio.GetMusic(Audio.MenuMusic, variant: 0));

        for (int variant = 0; variant < AssetSoundKey.BattleMusic.VariantCount; variant++)
        {
            AssetSoundKey.BattleMusic key = new(variant);

            yield return OptionalSound(key, source => source.Audio.GetMusic(Audio.BattleMusic, key.Variant));
        }

        for (int variant = 0; variant < AssetSoundKey.WorldMusic.VariantCount; variant++)
        {
            AssetSoundKey.WorldMusic key = new(variant);

            yield return OptionalSound(key, source => source.Audio.GetMusic(Audio.WorldMusic, key.Variant));
        }

        for (int variant = 0; variant < AssetSoundKey.AmbientMusic.VariantCount; variant++)
        {
            AssetSoundKey.AmbientMusic key = new(variant);

            yield return OptionalSound(key, source => source.Audio.GetMusic(Audio.AmbientMusic, key.Variant));
        }

        yield return OptionalSound(
            new AssetSoundKey.BriefingMusic(),
            source => source.Audio.GetMusic(Audio.BriefingMusic, variant: 0));

        foreach (ScenarioId scenarioId in scenarioIds)
        {
            AssetSoundKey.VictoryMusic victoryKey = new(scenarioId);
            AssetSoundKey.BriefingVoice voiceKey = new(scenarioId);

            yield return OptionalSound(victoryKey, source => source.Audio.GetVictoryMusic(victoryKey.ScenarioId));
            yield return OptionalSound(voiceKey, source => source.Audio.GetBriefingVoice(voiceKey.ScenarioId));
        }

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            for (int variant = 0; variant < AssetSoundKey.CapitalMusic.VariantCount; variant++)
            {
                AssetSoundKey.CapitalMusic key = new(race, variant);

                yield return OptionalSound(key, source => source.Audio.GetMusic(Audio.CapitalMusic(key.Race), key.Variant));
            }
        }

        yield return OptionalSound(
            new AssetSoundKey.BattleEffect.Heal(),
            source => source.Audio.GetBankSound(Audio.BattleHealSounds, variant: 0));

        yield return OptionalSound(
            new AssetSoundKey.BattleEffect.Death(),
            source => source.Audio.GetBankSound(Audio.BattleDeathSounds, variant: 0));

        foreach (AssetSoundKey.BattleEffect.StatusType status in Enum.GetValues<AssetSoundKey.BattleEffect.StatusType>())
        {
            AssetSoundKey.BattleEffect.Status key = new(status);

            yield return OptionalSound(key, source => source.Audio.GetBankSound(Audio.BattleStatusSounds(key.Type), variant: 0));
        }

        foreach (AssetSoundKey.BattleEffect.PotionType potion in Enum.GetValues<AssetSoundKey.BattleEffect.PotionType>())
        {
            AssetSoundKey.BattleEffect.Potion key = new(potion);

            yield return OptionalSound(key, source => source.Audio.GetBankSound(Audio.BattlePotionSounds(key.Type), variant: 0));
        }

        foreach (UnitDefinitionId unitId in unitIds)
        {
            for (int variant = 0; variant < AssetSoundKey.BattleUnitAttack.VariantCount; variant++)
            {
                AssetSoundKey.BattleUnitAttack key = new(unitId, variant);

                yield return OptionalSound(
                    key,
                    source => source.Audio.GetUnitSound(key.DefinitionId, UnitSoundType.Attack, key.Variant));
            }

            for (int variant = 0; variant < AssetSoundKey.BattleUnitHit.VariantCount; variant++)
            {
                AssetSoundKey.BattleUnitHit key = new(unitId, variant);

                yield return OptionalSound(
                    key,
                    source => source.Audio.GetUnitSound(key.DefinitionId, UnitSoundType.Hit, key.Variant));
            }

            for (int variant = 0; variant < AssetSoundKey.BattleUnitTouch.VariantCount; variant++)
            {
                AssetSoundKey.BattleUnitTouch key = new(unitId, variant);

                yield return OptionalSound(
                    key,
                    source => source.Audio.GetUnitSound(key.DefinitionId, UnitSoundType.Touch, key.Variant));
            }

            for (int variant = 0; variant < AssetSoundKey.BattleUnitDamaged.VariantCount; variant++)
            {
                AssetSoundKey.BattleUnitDamaged key = new(unitId, variant);

                yield return OptionalSound(
                    key,
                    source => source.Audio.GetUnitSound(key.DefinitionId, UnitSoundType.Damaged, key.Variant));
            }

            for (int variant = 0; variant < AssetSoundKey.StackUnitMove.VariantCount; variant++)
            {
                AssetSoundKey.StackUnitMove key = new(unitId, variant);

                yield return OptionalSound(
                    key,
                    source => source.Audio.GetUnitSound(key.DefinitionId, UnitSoundType.Walk, key.Variant));
            }

            for (int variant = 0; variant < AssetSoundKey.StackUnitResponse.VariantCount; variant++)
            {
                AssetSoundKey.StackUnitResponse key = new(unitId, variant);

                yield return OptionalSound(key, source => source.Audio.GetBankSound(Audio.StackUnitResponseSounds(key.DefinitionId), key.Variant));
            }

            AssetSoundKey.StackUnitAttack attackKey = new(unitId);

            yield return OptionalSound(
                attackKey,
                source => source.Audio.GetBankSound(Audio.StackUnitAttackSounds(attackKey.DefinitionId), variant: 0));

            for (int variant = 0; variant < AssetSoundKey.StackUnitIdle.VariantCount; variant++)
            {
                AssetSoundKey.StackUnitIdle key = new(unitId, variant);

                yield return OptionalSound(key, source => source.Audio.GetBankSound(Audio.StackUnitIdleSounds(key.DefinitionId), key.Variant));
            }

            AssetSoundKey.StackUnitDeath deathKey = new(unitId);

            yield return OptionalSound(
                deathKey,
                source => source.Audio.GetBankSound(Audio.StackUnitDeathSounds(deathKey.DefinitionId), variant: 0));
        }

        foreach (WorldSpellDefinitionId spellId in spellIds)
        {
            AssetSoundKey.WorldSpellCast key = new(spellId);

            yield return OptionalSound(key, source => source.Audio.GetBankSound(Spells.CastSounds(key.SpellId), variant: 0));
        }

        foreach (AssetSoundKey.AmbientSoundType ambient in Enum.GetValues<AssetSoundKey.AmbientSoundType>())
        {
            AssetSoundKey.Ambient key = new(ambient);

            yield return OptionalSound(key, source => source.Audio.GetBankSound(Audio.AmbientSounds(key.Type), variant: 0));
        }

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            AssetSoundKey.AmbientCapital key = new(race);

            yield return OptionalSound(key, source => source.Audio.GetBankSound(Audio.AmbientCapitalSounds(key.Race), variant: 0));
        }

        for (int variant = 0; variant < AssetSoundKey.AmbientRuin.VariantCount; variant++)
        {
            AssetSoundKey.AmbientRuin key = new(variant);

            yield return OptionalSound(key, source => source.Audio.GetBankSound(Audio.AmbientRuinSounds, key.Variant));
        }
    }

    private AssetSound? GetVictoryMusic(ScenarioId scenarioId)
    {
        if (music is not null && database.VictoryMusic.TryGetValue(scenarioId, out string? name))
        {
            return PickVariant(music.GetMusic(Audio.VictoryMusic(name)), variant: 0);
        }
        else
        {
            return null;
        }
    }

    private AssetSound? GetBriefingVoice(ScenarioId scenarioId)
    {
        if (briefingVoices is null)
        {
            return null;
        }

        if (database.BriefingVoices.TryGetValue(scenarioId, out int number))
        {
            return GetBriefingRecording(briefingVoices, number);
        }
        else if (database.BriefingVideos.TryGetValue(scenarioId, out string? video))
        {
            return GetBriefingVideoSound(briefingVoices, video);
        }
        else
        {
            return null;
        }
    }

    private static AssetSound? GetBriefingRecording(GameMusicSource briefingVoices, int number)
    {
        string name = Audio.BriefingVoice(number);
        AssetSound? voice = PickVariant(briefingVoices.GetMusic([name]), variant: 0);

        if (voice is null)
        {
            return null;
        }

        return WithVoiceStartInfo(voice, name);
    }

    private static AssetSound? GetBriefingVideoSound(GameMusicSource briefingVoices, string video)
    {
        string name = Audio.BriefingVideo(video);
        AssetSound? sound = PickVariant(briefingVoices.GetMusic([name]), variant: 0);

        if (sound is null)
        {
            return null;
        }

        using Stream stream = sound.Data.Open();
        byte[]? wave = BinkAudioExtractor.ExtractWave(stream);

        if (wave is null)
        {
            return null;
        }

        return WithVoiceStartInfo(sound with { Data = AssetStream.FromBytes(wave) }, name);
    }

    private static AssetSound WithVoiceStartInfo(AssetSound voice, string name)
    {
        TimeSpan startTime = BriefingVoiceStarts.ByName[name];

        return voice with { Metadata = voice.Metadata with { StartTime = startTime } };
    }

    private AssetSound? GetMusic(IReadOnlyList<string> names, int variant)
    {
        if (music is not null)
        {
            return PickVariant(music.GetMusic(names), variant);
        }
        else
        {
            return null;
        }
    }

    private AssetSound? GetBankSound(IReadOnlyList<string> names, int variant)
    {
        if (sounds is not null)
        {
            return PickVariant(sounds.GetSounds(names), variant);
        }
        else
        {
            return null;
        }
    }

    private AssetSound? GetUnitSound(UnitDefinitionId unitDefinitionId, UnitSoundType type, int variant)
    {
        if (sounds is not null)
        {
            UnitDefinitionId soundUnitId = type switch
            {
                UnitSoundType.Attack or UnitSoundType.Hit or UnitSoundType.Touch or UnitSoundType.Damaged => ResolveRecordUnitId(unitDefinitionId),
                UnitSoundType.Walk => ResolveWalkSoundUnitId(sounds, unitDefinitionId)
            };

            return PickVariant(sounds.GetUnitSounds(soundUnitId.Value, type), variant);
        }
        else
        {
            return null;
        }
    }

    private static AssetSound? PickVariant(IReadOnlyList<AssetSound>? variants, int variant)
    {
        if (variants is { Count: > 0 })
        {
            return variants[variant.Mod(variants.Count)];
        }
        else
        {
            return null;
        }
    }

    public GameUnitAttackTriggers GetUnitAttackTriggers(UnitDefinitionId unitDefinitionId)
    {
        if (sounds is null)
        {
            throw new InvalidOperationException($"Unit '{unitDefinitionId}' has no attack triggers without the battle sound mapping.");
        }

        UnitDefinitionId recordUnitId = ResolveRecordUnitId(unitDefinitionId);

        return sounds.GetAttackTriggers(recordUnitId.Value);
    }

    private UnitDefinitionId ResolveRecordUnitId(UnitDefinitionId unitDefinitionId)
    {
        return database.BaseUnits.GetValueOrDefault(unitDefinitionId, unitDefinitionId);
    }

    private UnitDefinitionId ResolveWalkSoundUnitId(GameUnitSoundsSource source, UnitDefinitionId unitDefinitionId)
    {
        UnitDefinitionId current = unitDefinitionId;

        for (int step = 0; step < 8; step++)
        {
            if (source.HasUnitSounds(current.Value, UnitSoundType.Walk))
            {
                break;
            }

            if (database.BaseUnits.TryGetValue(current, out UnitDefinitionId next))
            {
                current = next;
            }
            else
            {
                break;
            }
        }

        return current;
    }
}
