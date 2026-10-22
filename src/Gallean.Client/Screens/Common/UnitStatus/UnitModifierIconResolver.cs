using Gallean.Engine;

namespace Gallean.Client;

internal static class UnitModifierIconResolver
{
    public static IconMoniker Resolve(UnitModifierDefinition modifier)
    {
        LeaderAbility? ability = FindFirstAbility(modifier.LeaderAbilities);

        if (ability is not null)
        {
            return LeaderAbilityIconResolver.Resolve(ability.Value);
        }
        else if (modifier.Wards.Any(ward => ward.Category is UnitWardCategory.Always))
        {
            return IconMoniker.Immunity;
        }
        else if (modifier.Wards.Length > 0)
        {
            return IconMoniker.Ward;
        }
        else if (modifier.MoveAbility != UnitMoveAbility.None)
        {
            return IconMoniker.Boot;
        }
        else if (HasValue(modifier.Leadership))
        {
            return IconMoniker.Leadership;
        }
        else if (HasValue(modifier.MaxMovement))
        {
            return IconMoniker.Movement;
        }
        else if (HasValue(modifier.VisionRadius))
        {
            return IconMoniker.Vision;
        }
        else if (HasValue(modifier.Hitpoints) || modifier.DrainPercent > 0)
        {
            return IconMoniker.Hitpoint;
        }
        else if (modifier.RegenerationPercent > 0)
        {
            return IconMoniker.Heal;
        }
        else if (HasValue(modifier.Armor))
        {
            return IconMoniker.Armor;
        }
        else if (HasValue(modifier.Damage))
        {
            return IconMoniker.Damage;
        }
        else if (HasValue(modifier.Accuracy))
        {
            return IconMoniker.Accuracy;
        }
        else if (HasValue(modifier.Initiative))
        {
            return IconMoniker.Initiative;
        }
        else if (modifier.FastRetreat)
        {
            return IconMoniker.Retreat;
        }
        else if (modifier.PurchaseDiscountPercent > 0)
        {
            return IconMoniker.Gold;
        }
        else
        {
            return IconMoniker.LearnAbility;
        }
    }

    private static LeaderAbility? FindFirstAbility(LeaderAbility abilities)
    {
        foreach (LeaderAbility ability in Enum.GetValues<LeaderAbility>())
        {
            if (ability != LeaderAbility.None && abilities.HasFlag(ability))
            {
                return ability;
            }
        }

        return null;
    }

    private static bool HasValue(UnitStatModifier stat)
    {
        return stat.Flat > 0 || stat.Percent > 0;
    }
}
