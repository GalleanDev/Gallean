using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class LeaderEquipmentCalculator
{
    public static ImmutableArray<UnitAttackDefinitionId> GetEquipmentAttackIds(
        LeaderEquipment equipment,
        IGameDefinitionProvider definitions)
    {
        ImmutableArray<UnitAttackDefinitionId>.Builder attackIds = ImmutableArray.CreateBuilder<UnitAttackDefinitionId>();

        foreach (WorldItem item in equipment.Items.Values)
        {
            if (definitions.GetItem(item.DefinitionId).EquipmentAttackId is { } attackId)
            {
                attackIds.Add(attackId);
            }
        }

        return attackIds.ToImmutable();
    }

    public static UnitGrid<WorldUnit> UpdateUnitModifiers(
        UnitGrid<WorldUnit> units,
        LeaderEquipment equipment,
        IGameDefinitionProvider definitions)
    {
        UnitGrid<WorldUnit> result = units;

        foreach ((WorldUnit unit, _) in units.GetUnits())
        {
            bool isLeader = definitions.GetUnit(unit.DefinitionId).Leadership is > 0;

            ImmutableArray<UnitModifier> modifiers = unit.Modifiers
                .RemoveAll(modifier => modifier.Source is UnitModifierSource.Equipment)
                .AddRange(GetEquipmentModifiers(equipment, isLeader, definitions));

            result = result.Replace(unit, unit with { Modifiers = modifiers });
        }

        return result;
    }

    private static ImmutableArray<UnitModifier> GetEquipmentModifiers(
        LeaderEquipment equipment,
        bool isLeader,
        IGameDefinitionProvider definitions)
    {
        ImmutableArray<UnitModifier>.Builder modifiers = ImmutableArray.CreateBuilder<UnitModifier>();

        foreach (WorldItem item in equipment.Items.Values)
        {
            ItemDefinition definition = definitions.GetItem(item.DefinitionId);

            if (definition.EquipmentModifierId is { } modifierId)
            {
                if (AppliesToStack(definition.Category) || isLeader)
                {
                    modifiers.Add(UnitModifier.FromEquipment(modifierId));
                }
            }
        }

        return modifiers.ToImmutable();
    }

    private static bool AppliesToStack(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Banner => true,
            ItemCategory.Weapon => false,
            ItemCategory.Armor => false,
            ItemCategory.Jewel => false,
            ItemCategory.TravelItem => false,
            ItemCategory.Orb => false,
            ItemCategory.Talisman => false,
            ItemCategory.Scroll => false,
            ItemCategory.Wand => false,
            ItemCategory.PotionTemporaryBoost => false,
            ItemCategory.PotionHeal => false,
            ItemCategory.PotionRevive => false,
            ItemCategory.PotionPermanentBoost => false,
            ItemCategory.Valuable => false,
            ItemCategory.Special => false
        };
    }

    public static bool CanPlace(ItemCategory category, LeaderEquipmentSlot slot)
    {
        return category switch
        {
            ItemCategory.Banner => slot == LeaderEquipmentSlot.Banner,
            ItemCategory.TravelItem => slot == LeaderEquipmentSlot.Boots,
            ItemCategory.Weapon => IsArtifactSlot(slot),
            ItemCategory.Armor => IsArtifactSlot(slot),
            ItemCategory.Jewel => slot == LeaderEquipmentSlot.Tome,
            ItemCategory.Orb => IsBattleSlot(slot),
            ItemCategory.Talisman => IsBattleSlot(slot),
            ItemCategory.Scroll => IsBattleSlot(slot),
            ItemCategory.Wand => IsBattleSlot(slot),
            ItemCategory.PotionTemporaryBoost => IsBattleSlot(slot),
            ItemCategory.PotionHeal => IsBattleSlot(slot),
            ItemCategory.PotionRevive => IsBattleSlot(slot),
            ItemCategory.PotionPermanentBoost => IsBattleSlot(slot),
            ItemCategory.Valuable => false,
            ItemCategory.Special => false
        };
    }

    public static bool IsConsumable(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.PotionTemporaryBoost => true,
            ItemCategory.PotionHeal => true,
            ItemCategory.PotionRevive => true,
            ItemCategory.PotionPermanentBoost => true,
            ItemCategory.Scroll => true,
            ItemCategory.Orb => false,
            ItemCategory.Talisman => false,
            ItemCategory.Wand => false,
            ItemCategory.Weapon => false,
            ItemCategory.Armor => false,
            ItemCategory.Jewel => false,
            ItemCategory.Banner => false,
            ItemCategory.TravelItem => false,
            ItemCategory.Valuable => false,
            ItemCategory.Special => false
        };
    }

    public static LeaderAbility? GetRequiredAbility(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Weapon => LeaderAbility.WeaponArmorUse,
            ItemCategory.Armor => LeaderAbility.WeaponArmorUse,
            ItemCategory.Jewel => LeaderAbility.JewelryUse,
            ItemCategory.Banner => LeaderAbility.BannerUse,
            ItemCategory.Orb => LeaderAbility.OrbUse,
            ItemCategory.Talisman => LeaderAbility.TalismanUse,
            ItemCategory.TravelItem => LeaderAbility.TravelItemUse,
            ItemCategory.Scroll => LeaderAbility.WandScrollUse,
            ItemCategory.Wand => LeaderAbility.WandScrollUse,
            ItemCategory.PotionTemporaryBoost => null,
            ItemCategory.PotionHeal => null,
            ItemCategory.PotionRevive => null,
            ItemCategory.PotionPermanentBoost => null,
            ItemCategory.Valuable => null,
            ItemCategory.Special => null
        };
    }

    private static bool IsArtifactSlot(LeaderEquipmentSlot slot)
    {
        return slot switch
        {
            LeaderEquipmentSlot.Artifact1 => true,
            LeaderEquipmentSlot.Artifact2 => true,
            LeaderEquipmentSlot.Banner => false,
            LeaderEquipmentSlot.Tome => false,
            LeaderEquipmentSlot.Battle1 => false,
            LeaderEquipmentSlot.Battle2 => false,
            LeaderEquipmentSlot.Boots => false
        };
    }

    private static bool IsBattleSlot(LeaderEquipmentSlot slot)
    {
        return slot switch
        {
            LeaderEquipmentSlot.Battle1 => true,
            LeaderEquipmentSlot.Battle2 => true,
            LeaderEquipmentSlot.Banner => false,
            LeaderEquipmentSlot.Tome => false,
            LeaderEquipmentSlot.Artifact1 => false,
            LeaderEquipmentSlot.Artifact2 => false,
            LeaderEquipmentSlot.Boots => false
        };
    }
}
