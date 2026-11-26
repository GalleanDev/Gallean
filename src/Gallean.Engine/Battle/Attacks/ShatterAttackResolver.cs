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

        if (GetShatteredArmor(context, target, attackStats) == 0)
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
        int shatteredArmor = GetShatteredArmor(context, target, attackStats);

        if (shatteredArmor != 0)
        {
            return [new BattleUnitEffectAppliedEvent(attackerId, targetId, new BattleShatterArmorStatusEffect(shatteredArmor))];
        }
        else
        {
            return [];
        }
    }

    private static int GetShatteredArmor(IEngineContext context, BattleUnit target, UnitAttackStats attackStats)
    {
        BattleShatterArmorStatusEffect? shatterEffect = target.StatusEffects.OfType<BattleShatterArmorStatusEffect>().SingleOrDefault();
        int shatteredArmor = shatterEffect?.Armor ?? 0;

        BattleFortificationStatusEffect? fortificationEffect = target.StatusEffects.OfType<BattleFortificationStatusEffect>().SingleOrDefault();
        int fortificationArmor = fortificationEffect?.Armor ?? 0;

        UnitDefinition targetDefinition = context.Definitions.GetUnit(target.DefinitionId);
        UnitStats unitStats = UnitStatsCalculator.GetStats(targetDefinition, target.Level, target.Modifiers, context.Definitions, context.Globals);

        int result = attackStats.Damage;
        if (result > 0)
        {
            int remainingArmor = Math.Max(0, unitStats.Armor - shatteredArmor - fortificationArmor);
            int remainingTotalCap = Math.Max(0, context.Globals.ShatterArmorTotalCap - shatteredArmor);

            result = Math.Min(result, context.Globals.ShatterArmorPerHitCap);
            result = Math.Min(result, remainingArmor);
            result = Math.Min(result, remainingTotalCap);
        }

        return result;
    }
}
