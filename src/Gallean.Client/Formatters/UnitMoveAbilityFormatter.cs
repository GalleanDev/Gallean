using Gallean.Engine;

namespace Gallean.Client;

internal static class UnitMoveAbilityFormatter
{
    public static string? Format(UnitMoveAbility ability)
    {
        return ability switch
        {
            UnitMoveAbility.None => null,
            UnitMoveAbility.Forest => Tr("Forest movement"),
            UnitMoveAbility.Water => Tr("Water movement"),
            UnitMoveAbility.Flying => Tr("Flight")
        };
    }
}
