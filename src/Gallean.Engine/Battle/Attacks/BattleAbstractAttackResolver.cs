namespace Gallean.Engine;

internal abstract class BattleAbstractAttackResolver : IBattleAttackResolver
{
    protected virtual bool DistributesDrainOverflow => false;

    public BattleAttackResult Resolve(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleAttackTarget target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        BattleUnitId? targetUnitId = target.GetUnitId();
        if (targetUnitId is null)
        {
            return BattleAttackResult.Fail(BattleCommandError.AttackRequiresUnitTarget);
        }

        BattleUnitId targetId = targetUnitId.Value;
        BattleUnit targetUnit = state.GetUnit(targetId);

        UnitAttackTypeInfo info = attack.Type.GetInfo();

        if (info.TargetsDead && targetUnit.IsAlive)
        {
            return BattleAttackResult.Fail(BattleCommandError.TargetNotDead);
        }

        if (!info.TargetsDead && !targetUnit.IsActive())
        {
            return BattleAttackResult.Fail(BattleCommandError.TargetDead);
        }

        BattleCommandError? error = ValidateTarget(state, context, attackerId, targetUnit, attack);
        if (error is not null)
        {
            return BattleAttackResult.Fail(error.Value);
        }

        List<BattleEvent> events = [new BattleUnitAttackedEvent(attackerId, targetId, attack.Id)];
        bool didHit;

        if (!info.CanMiss || HitCalculator.RollHit(attackStats.Accuracy, context.Random))
        {
            UnitWard? ward;

            if (info.IsWardable)
            {
                ward = BattleUnitWardCalculator.FindBlockingWard(targetUnit, attack);
            }
            else
            {
                ward = null;
            }

            if (ward is not null)
            {
                events.Add(new BattleUnitAttackBlockedEvent(attackerId, targetId, attack.Id, ward));

                if (ward.Category == UnitWardCategory.Once)
                {
                    events.Add(new BattleUnitWardConsumedEvent(targetId, ward));
                }

                didHit = false;
            }
            else
            {
                int damage = GetHitDamage(state, context, attackerId, targetId, attack, attackStats);
                int drainedHitpoints = Math.Min(damage, targetUnit.Hitpoints);
                int drainHeal = (drainedHitpoints * GetAttackDrainPercent(context.Globals) / 100) + (drainedHitpoints * attackStats.DrainPercent / 100);

                events.Add(new BattleUnitAttackHitEvent(attackerId, targetId, attack.Id, damage));
                events.AddRange(OnHit(state, context, attackerId, targetId, attack, attackStats));
                events.AddRange(ResolveDrain(state, context, attackerId, drainHeal, DistributesDrainOverflow));

                didHit = true;
            }
        }
        else
        {
            events.Add(new BattleUnitAttackMissedEvent(attackerId, targetId, attack.Id));

            didHit = false;
        }

        return new BattleAttackResult(events, didHit, Error: null);
    }

    protected virtual int GetAttackDrainPercent(GlobalVariables globals)
    {
        return 0;
    }

    protected virtual BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack)
    {
        return null;
    }

    protected virtual int GetHitDamage(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnitId targetId,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        return 0;
    }

    protected abstract IReadOnlyList<BattleEvent> OnHit(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnitId targetId,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats);

    private static IReadOnlyList<BattleEvent> ResolveDrain(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        int heal,
        bool distributesOverflow)
    {
        List<BattleEvent> events = [];

        if (heal > 0)
        {
            BattleUnit attacker = state.GetUnit(attackerId);
            int attackerMaxHitpoints = context.UnitStats.GetBattleUnitStats(state, attackerId).MaxHitpoints;
            int attackerHitpointsAfter = Math.Min(attacker.Hitpoints + heal, attackerMaxHitpoints);

            if (attackerHitpointsAfter > attacker.Hitpoints)
            {
                events.Add(new BattleUnitHealedEvent(attackerId, attackerHitpointsAfter));
            }

            if (distributesOverflow)
            {
                int overflow = heal - (attackerHitpointsAfter - attacker.Hitpoints);
                events.AddRange(DistributeDrainOverflow(state, context, attacker, overflow));
            }
        }

        return events;
    }

    private static IReadOnlyList<BattleEvent> DistributeDrainOverflow(
        BattleState state,
        IEngineContext context,
        BattleUnit attacker,
        int overflow)
    {
        if (overflow <= 0)
        {
            return [];
        }

        UnitGrid<BattleUnit> unitGrid = state.GetGrid(attacker.Side);
        List<BattleUnit> targetUnits = [];
        List<int> capacities = [];

        foreach ((BattleUnit unit, _) in unitGrid.GetUnits())
        {
            if (unit.Id != attacker.Id && unit.IsActive())
            {
                int maxHitpoints = context.UnitStats.GetBattleUnitStats(state, unit.Id).MaxHitpoints;
                if (unit.Hitpoints < maxHitpoints)
                {
                    targetUnits.Add(unit);
                    capacities.Add(maxHitpoints - unit.Hitpoints);
                }
            }
        }

        List<BattleEvent> events = [];
        IReadOnlyList<int> healed = DrainOverflowCalculator.Distribute(overflow, capacities);

        for (int i = 0; i < targetUnits.Count; i++)
        {
            if (healed[i] > 0)
            {
                BattleUnit unit = targetUnits[i];
                events.Add(new BattleUnitHealedEvent(unit.Id, unit.Hitpoints + healed[i]));
            }
        }

        return events;
    }
}
