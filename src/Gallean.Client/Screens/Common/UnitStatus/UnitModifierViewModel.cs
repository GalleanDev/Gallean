using Avalonia.Media;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record UnitModifierViewModel(string Effects, TextSpans? Duration, UnitModifierType Type)
{
    public Color Color => Type switch
    {
        UnitModifierType.Boost => Theme.Battle.StatusEffect.BoostModifierColor,
        UnitModifierType.Weaken => Theme.Battle.StatusEffect.WeakenModifierColor
    };

    public static IReadOnlyList<UnitModifierViewModel> CreateAll(WorldUnit unit, IGameDefinitionProvider definitions)
    {
        List<UnitModifierViewModel> result = [];

        foreach (UnitModifier modifier in unit.Modifiers)
        {
            if (modifier.RemainingTurns is { } remainingTurns)
            {
                result.Add(Create(definitions.GetUnitModifier(modifier.DefinitionId), TrSpans("Turns left: {0:*}", remainingTurns)));
            }
        }

        return result;
    }

    public static IReadOnlyList<UnitModifierViewModel> CreateAll(BattleUnit unit, IGameDefinitionProvider definitions)
    {
        List<UnitModifierViewModel> result = [];

        foreach (UnitModifier modifier in unit.Modifiers)
        {
            if (modifier.RemainingTurns is not null)
            {
                result.Add(Create(definitions.GetUnitModifier(modifier.DefinitionId), null));
            }
        }

        foreach (BattleUnitWardModifier wardModifier in unit.WardModifiers)
        {
            result.Add(Create(definitions.GetUnitModifier(wardModifier.ModifierId), TrSpans("Until the caster's next turn")));
        }

        return result;
    }

    private static UnitModifierViewModel Create(UnitModifierDefinition definition, TextSpans? duration)
    {
        return new UnitModifierViewModel(
            string.Join(", ", UnitModifierFormatter.FormatEffects(definition)),
            duration,
            ResolveType(definition));
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
