namespace Gallean.Engine;

internal interface IBattleAttackResolver
{
    bool EndsTurn => true;

    bool CanUseAlternativeAttack(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        UnitAttackDefinition attack) => true;

    BattleAttackResult Resolve(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleAttackTarget target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats);
}
