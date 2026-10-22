using Gallean.Engine.Contracts;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed class GameGraphicsResolver(GameDatabase database, GameImages units, GameImages battleUnits)
{
    public UnitDefinitionId ResolveUnitMapGraphicsId(UnitDefinitionId unitId)
    {
        UnitDefinitionId current = unitId;

        for (int step = 0; step < 8; step++)
        {
            if (units.HasAnimation(Stacks.Unit(current.Value, AssetAnimationKey.StackUnitPose.Move, 0)))
            {
                break;
            }

            if (database.BaseUnits.TryGetValue(current, out UnitDefinitionId next))
            {
                current = next;
            }
            else
            {
                break;
            }
        }

        return current;
    }

    public UnitDefinitionId ResolveUnitFaceGraphicsId(UnitDefinitionId unitId)
    {
        UnitDefinitionId current = unitId;

        for (int step = 0; step < 8; step++)
        {
            if (!database.BaseUnits.TryGetValue(current, out UnitDefinitionId next))
            {
                break;
            }

            current = next;
        }

        return current;
    }

    public UnitDefinitionId ResolveUnitBattleGraphicsId(UnitDefinitionId unitId)
    {
        UnitDefinitionId current = unitId;

        for (int step = 0; step < 8; step++)
        {
            if (battleUnits.HasAnimation(Battle.Unit(current.Value, AssetAnimationKey.BattleUnitPose.Idle, AssetAnimationKey.BattleLayer.Body, BattleSide.Attacker)) ||
                battleUnits.HasAnimation(Battle.Unit(current.Value, AssetAnimationKey.BattleUnitPose.Idle, AssetAnimationKey.BattleLayer.Body, BattleSide.Defender)))
            {
                break;
            }

            if (database.BaseUnits.TryGetValue(current, out UnitDefinitionId next))
            {
                current = next;
            }
            else
            {
                break;
            }
        }

        return current;
    }
}
