using Gallean.Engine;

namespace Gallean.Client;

internal static class UnitModifierFormatter
{
    public static IEnumerable<string> FormatEffects(UnitModifierDefinition modifier, IGameTextProvider texts)
    {
        foreach (string value in FormatValues(modifier.Hitpoints))
        {
            yield return Tr("{0} hit points", value);
        }

        foreach (string value in FormatValues(modifier.Armor))
        {
            yield return Tr("{0} armor", value);
        }

        foreach (string value in FormatValues(modifier.Damage))
        {
            yield return Tr("{0} damage", value);
        }

        foreach (string value in FormatValues(modifier.Accuracy))
        {
            yield return Tr("{0} accuracy", value);
        }

        foreach (string value in FormatValues(modifier.Initiative))
        {
            yield return Tr("{0} initiative", value);
        }

        if (modifier.RegenerationPercent != 0)
        {
            yield return Tr("{0} regeneration", $"{modifier.RegenerationPercent:+0;-0}%");
        }

        foreach (string value in FormatValues(modifier.Leadership))
        {
            yield return Tr("{0} leadership", value);
        }

        foreach (string value in FormatValues(modifier.MaxMovement))
        {
            yield return Tr("{0} movement", value);
        }

        foreach (string value in FormatValues(modifier.VisionRadius))
        {
            yield return Tr("{0} vision", value);
        }

        string? moveAbility = FormatMoveAbility(modifier.MoveAbility);

        if (moveAbility is not null)
        {
            yield return moveAbility;
        }

        foreach (UnitWard ward in modifier.Wards)
        {
            yield return FormatWard(ward);
        }

        if (modifier.DrainPercent != 0)
        {
            yield return Tr("Drain life {0}%", modifier.DrainPercent);
        }

        if (modifier.FastRetreat)
        {
            yield return Tr("Instant retreat");
        }

        if (modifier.PurchaseDiscountPercent != 0)
        {
            yield return Tr("{0}% lower prices", modifier.PurchaseDiscountPercent);
        }

        foreach (string ability in LeaderAbilityFormatter.Format(modifier.LeaderAbilities, texts))
        {
            yield return ability;
        }
    }

    private static IEnumerable<string> FormatValues(UnitStatModifier stat)
    {
        if (stat.Flat != 0)
        {
            yield return $"{stat.Flat:+0;-0}";
        }

        if (stat.Percent != 0)
        {
            yield return $"{stat.Percent:+0;-0}%";
        }
    }

    private static string? FormatMoveAbility(UnitMoveAbility ability)
    {
        return ability switch
        {
            UnitMoveAbility.None => null,
            UnitMoveAbility.Forest => Tr("Forest movement"),
            UnitMoveAbility.Water => Tr("Water movement"),
            UnitMoveAbility.Flying => Tr("Flight")
        };
    }

    private static string FormatWard(UnitWard ward)
    {
        string name = UnitWardFormatter.FormatName(ward);

        return ward.Category switch
        {
            UnitWardCategory.Once => Tr("Ward against {0}", name),
            UnitWardCategory.Always => Tr("Immune to {0}", name)
        };
    }
}
