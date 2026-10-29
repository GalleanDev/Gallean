using System.Collections.Immutable;

namespace Gallean.Engine;

public interface IUnitStatsProvider
{
    UnitStats GetBattleUnitStats(BattleState state, BattleUnitId unitId);
    int GetBattleUnitInitiative(BattleState state, BattleUnitId unitId, int roll);
    UnitAttackStats GetBattleAttackStats(BattleState state, BattleUnitId unitId, UnitAttackDefinition attack);
    LeaderAbility GetBattleUnitAbilities(BattleState state, BattleUnitId unitId);
    int GetBattleUnitLeadership(BattleState state, BattleUnitId unitId);
    UnitExperience GetBattleUnitExperience(BattleState state, BattleUnitId unitId);

    StackStats GetWorldStackStats(WorldStack stack);
    UnitStats GetWorldUnitStats(WorldUnit unit);
    UnitAttackStats GetWorldAttackStats(WorldUnit unit, UnitAttackDefinition attack);
    LeaderAbility GetWorldUnitAbilities(WorldUnit unit);
    int GetWorldUnitLeadership(WorldUnit unit);
    ImmutableArray<UnitWard> GetWorldUnitWards(WorldUnit unit);
    UnitExperience GetWorldUnitExperience(WorldUnit unit);

    UnitUpgrade GetUnitUpgrade(UnitDefinitionId unitDefinitionId, bool isTypeLocked, WorldPlayerBuildings buildings);
    UnitCosts GetUnitCosts(UnitDefinitionId unitDefinitionId, int level);
}
