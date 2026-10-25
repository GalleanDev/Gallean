using Gallean.Engine.Contracts;

namespace Packer;

internal static class SoundRecordMap
{
    private static readonly IReadOnlyDictionary<UnitDefinitionId, UnitDefinitionId> RecordUnits = new Dictionary<UnitDefinitionId, UnitDefinitionId>
    {
        [new UnitDefinitionId("g000uu8006")] = new("g000uu5008"),
        [new UnitDefinitionId("g000uu8007")] = new("g000uu6107"),
        [new UnitDefinitionId("g000uu8008")] = new("g000uu0157")
    };

    public static AssetKey? GetRecordKey(AssetKey key)
    {
        return key switch
        {
            AssetSoundKey.BattleUnitAttack sound when RecordUnits.TryGetValue(sound.DefinitionId, out UnitDefinitionId recordUnit) => sound with { DefinitionId = recordUnit },
            AssetSoundKey.BattleUnitHit sound when RecordUnits.TryGetValue(sound.DefinitionId, out UnitDefinitionId recordUnit) => sound with { DefinitionId = recordUnit },
            AssetSoundKey.BattleUnitTouch sound when RecordUnits.TryGetValue(sound.DefinitionId, out UnitDefinitionId recordUnit) => sound with { DefinitionId = recordUnit },
            AssetSoundKey.BattleUnitDamaged sound when RecordUnits.TryGetValue(sound.DefinitionId, out UnitDefinitionId recordUnit) => sound with { DefinitionId = recordUnit },
            AssetSoundKey.StackUnitMove sound when RecordUnits.TryGetValue(sound.DefinitionId, out UnitDefinitionId recordUnit) => sound with { DefinitionId = recordUnit },
            AssetAnimationKey.BattleUnit { Pose: AssetAnimationKey.BattleUnitPose.Attack } unit when RecordUnits.TryGetValue(unit.DefinitionId, out UnitDefinitionId recordUnit) => unit with { DefinitionId = recordUnit },
            AssetAnimationKey.BattleEffect.Attack { Layer: AssetAnimationKey.BattleLayer.Body } effect when RecordUnits.TryGetValue(effect.AttackerUnitId, out UnitDefinitionId recordUnit) => effect with { AttackerUnitId = recordUnit },
            _ => null
        };
    }
}
