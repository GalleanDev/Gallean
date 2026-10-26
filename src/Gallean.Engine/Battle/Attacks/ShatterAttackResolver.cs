using Gallean.Common;

namespace Gallean.Engine;

internal sealed class ShatterAttackResolver : BattleAbstractAttackResolver, IStaticInstance<ShatterAttackResolver>
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        if (target.IsRetreating())
        {
            return BattleCommandError.TargetIncorrect;
        }

        if (GetShatteredArmor(context, target, attack) == 0)
        {
            return BattleCommandError.TargetIncorrect;
        }

        return null;
    }

    protected override IReadOnlyList<BattleEvent> OnHit(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnitId targetId,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        BattleUnit target = state.GetUnit(targetId);
        int shatteredArmor = GetShatteredArmor(context, target, attack);

        if (shatteredArmor != 0)
        {
            return [new BattleUnitEffectAppliedEvent(attackerId, targetId, new BattleShatterArmorStatusEffect(shatteredArmor))];
        }
        else
        {
            return [];
        }
    }

    private static int GetShatteredArmor(IEngineContext context, BattleUnit target, UnitAttackDefinition attack)
    {
        int alreadyShattered = 0;
        foreach (BattleShatterArmorStatusEffect effect in target.StatusEffects.OfType<BattleShatterArmorStatusEffect>())
        {
            alreadyShattered += effect.Armor;
        }

        UnitDefinition targetDefinition = context.Definitions.GetUnit(target.DefinitionId);
        UnitStats unitStats = UnitStatsCalculator.GetStats(targetDefinition, target.Level, target.Modifiers, context.Definitions, context.Globals);

        int shatteredArmor = attack.Damage;
        if (shatteredArmor > 0)
        {
            int remainingArmor = Math.Max(0, unitStats.Armor - alreadyShattered);
            int remainingTotalCap = Math.Max(0, context.Globals.ShatterArmorTotalCap - alreadyShattered);

            shatteredArmor = Math.Min(shatteredArmor, context.Globals.ShatterArmorPerHitCap);
            shatteredArmor = Math.Min(shatteredArmor, remainingArmor);
            shatteredArmor = Math.Min(shatteredArmor, remainingTotalCap);
        }

        return shatteredArmor;
    }
}
