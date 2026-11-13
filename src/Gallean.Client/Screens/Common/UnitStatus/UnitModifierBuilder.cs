using System.Diagnostics.CodeAnalysis;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class UnitModifierBuilder
{
    private UnitModifierDefinition? result;

    public UnitModifierBuilder Add(UnitModifierDefinition modifier)
    {
        if (result is null)
        {
            result = modifier;
        }
        else
        {
            result = result with
            {
                MoveAbility = result.MoveAbility | modifier.MoveAbility,
                MaxMovement = Add(result.MaxMovement, modifier.MaxMovement),
                Leadership = Add(result.Leadership, modifier.Leadership),
                VisionRadius = Add(result.VisionRadius, modifier.VisionRadius),
                Hitpoints = Add(result.Hitpoints, modifier.Hitpoints),
                RegenerationPercent = result.RegenerationPercent + modifier.RegenerationPercent,
                Armor = Add(result.Armor, modifier.Armor),
                Initiative = Add(result.Initiative, modifier.Initiative),
                Damage = Add(result.Damage, modifier.Damage),
                Accuracy = Add(result.Accuracy, modifier.Accuracy),
                DrainPercent = result.DrainPercent + modifier.DrainPercent,
                FastRetreat = result.FastRetreat || modifier.FastRetreat,
                PurchaseDiscountPercent = result.PurchaseDiscountPercent + modifier.PurchaseDiscountPercent,
                LeaderAbilities = result.LeaderAbilities | modifier.LeaderAbilities,
                Wards = [.. result.Wards.Union(modifier.Wards)]
            };
        }

        return this;
    }

    public UnitModifierBuilder AddRange(IEnumerable<UnitModifierDefinition> modifiers)
    {
        foreach (UnitModifierDefinition modifier in modifiers)
        {
            Add(modifier);
        }

        return this;
    }

    public bool TryBuild([NotNullWhen(true)] out UnitModifierDefinition? modifier)
    {
        modifier = result;

        return modifier is not null;
    }

    private static UnitStatModifier Add(UnitStatModifier stat, UnitStatModifier other)
    {
        return new UnitStatModifier(stat.Flat + other.Flat, stat.Percent + other.Percent);
    }
}
