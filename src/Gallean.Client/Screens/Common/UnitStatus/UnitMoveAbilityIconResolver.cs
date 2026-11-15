using Gallean.Engine;

namespace Gallean.Client;

internal static class UnitMoveAbilityIconResolver
{
    public static IconMoniker Resolve(UnitMoveAbility ability)
    {
        if (ability.HasFlag(UnitMoveAbility.Flying))
        {
            return IconMoniker.MoveAbilityFlying;
        }
        else if (ability.HasFlag(UnitMoveAbility.Forest))
        {
            return IconMoniker.MoveAbilityForest;
        }
        else if (ability.HasFlag(UnitMoveAbility.Water))
        {
            return IconMoniker.MoveAbilityWater;
        }
        else
        {
            return IconMoniker.Boot;
        }
    }
}
