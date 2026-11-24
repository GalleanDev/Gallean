using Avalonia.Media;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record UnitModifierViewModel(string Effects, TextSpans? Duration, UnitModifierCategory Category, UnitModifierType Type)
{
    public Color Color => Type switch
    {
        UnitModifierType.Boost => Theme.Battle.StatusEffect.BoostModifierColor,
        UnitModifierType.Weaken => Theme.Battle.StatusEffect.WeakenModifierColor
    };

    public static IReadOnlyList<UnitModifierViewModel> CreateAll(WorldUnit unit, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        List<UnitModifierViewModel> result = [];

        foreach (UnitModifier modifier in unit.Modifiers)
        {
            if (modifier.RemainingTurns is { } remainingTurns)
            {
                result.Add(Create(
                    definitions.GetUnitModifier(modifier.DefinitionId),
                    TrSpans("Turns left: {0:*}", remainingTurns),
                    ResolveCategory(modifier),
                    texts));
            }
        }

        return result;
    }

    public static IReadOnlyList<UnitModifierViewModel> CreateAll(BattleUnit unit, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        List<UnitModifierViewModel> result = [];

        foreach (UnitModifier modifier in unit.Modifiers)
        {
            if (modifier.RemainingTurns is not null)
            {
                result.Add(Create(definitions.GetUnitModifier(modifier.DefinitionId), null, ResolveCategory(modifier), texts));
            }
        }

        foreach (BattleUnitWardModifier wardModifier in unit.WardModifiers)
        {
            result.Add(Create(definitions.GetUnitModifier(wardModifier.ModifierId), TrSpans("Until the caster's next turn"), UnitModifierCategory.Magic, texts));
        }

        return result;
    }

    private static UnitModifierViewModel Create(UnitModifierDefinition definition, TextSpans? duration, UnitModifierCategory category, IGameTextProvider texts)
    {
        return new UnitModifierViewModel(
            string.Join(", ", UnitModifierFormatter.FormatEffects(definition, texts)),
            duration,
            category,
            ResolveType(definition));
    }

    private static UnitModifierCategory ResolveCategory(UnitModifier modifier)
    {
        return modifier.Source switch
        {
            UnitModifierSource.Unit => UnitModifierCategory.Magic,
            UnitModifierSource.Equipment => UnitModifierCategory.Magic,
            UnitModifierSource.Scenario => UnitModifierCategory.Magic,
            UnitModifierSource.Fortification => UnitModifierCategory.Magic,
            UnitModifierSource.Potion => UnitModifierCategory.Potion
        };
    }

    private static UnitModifierType ResolveType(UnitModifierDefinition definition)
    {
        UnitStatModifier[] stats =
        [
            definition.Hitpoints,
            definition.Armor,
            definition.Damage,
            definition.Accuracy,
            definition.Initiative,
            definition.Leadership,
            definition.MaxMovement,
            definition.VisionRadius
        ];

        if (stats.Any(stat => stat.Flat < 0 || stat.Percent < 0) || definition.RegenerationPercent < 0)
        {
            return UnitModifierType.Weaken;
        }
        else
        {
            return UnitModifierType.Boost;
        }
    }
}
