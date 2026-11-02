using Gallean.Engine;

namespace Gallean.Client;

internal static class UnitMoveAbilityIconResolver
{
    public static IconMoniker Resolve(UnitMoveAbility ability)
    {
        return ability switch
        {
            UnitMoveAbility.None => IconMoniker.Boot,
            UnitMoveAbility.Forest => IconMoniker.MoveAbilityForest,
            UnitMoveAbility.Water => IconMoniker.MoveAbilityWater,
            UnitMoveAbility.Flying => IconMoniker.MoveAbilityFlying
        };
    }
}
