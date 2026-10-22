using Gallean.Common;

namespace Gallean.Engine;

internal sealed class BattleItemResolver : IItemEffectVisitor<BattleItemResolver.Context, BattleItemCalculator.ItemInfo>, IStaticInstance<BattleItemResolver>
{
    internal sealed record Context(
        BattleState State,
        IEngineContext EngineContext,
        BattleUnit User,
        BattleAttackTarget Target);

    public BattleItemCalculator.ItemInfo Visit(HealItemEffect effect, Context context)
    {
        BattleUnitId? targetId = context.Target.GetUnitId();

        if (targetId is null)
        {
            return BattleItemCalculator.ItemInfo.Fail(BattleCommandError.ItemTargetInvalid);
        }

        BattleUnit target = context.State.GetUnit(targetId.Value);

        if (target.Side != context.User.Side || !target.IsActive())
        {
            return BattleItemCalculator.ItemInfo.Fail(BattleCommandError.ItemTargetInvalid);
        }

        UnitStats stats = context.EngineContext.UnitStats.GetBattleUnitStats(context.State, target.Id);

        if (target.Hitpoints >= stats.MaxHitpoints)
        {
            return BattleItemCalculator.ItemInfo.Fail(BattleCommandError.TargetNotHealable);
        }

        int hitpointsAfter = Math.Min(target.Hitpoints + effect.Hitpoints, stats.MaxHitpoints);

        return new BattleItemCalculator.ItemInfo([new BattleAttackUnitTarget(target.Id)], [new BattleUnitHealedEvent(target.Id, hitpointsAfter)], null);
    }

    public BattleItemCalculator.ItemInfo Visit(AttackItemEffect effect, Context context)
    {
        UnitAttackDefinition attack = context.EngineContext.Definitions.GetAttack(effect.AttackId);

        BattleAttackCalculator.AttackInfo info = BattleAttackCalculator.GetAttackInfo(
            context.State,
            context.EngineContext,
            context.User.Id,
            context.Target,
            attack);

        if (info.Failed)
        {
            return BattleItemCalculator.ItemInfo.Fail(info.Error.Value);
        }

        return new BattleItemCalculator.ItemInfo(info.Targets, info.Events, null);
    }

    public BattleItemCalculator.ItemInfo Visit(ReviveItemEffect effect, Context context)
    {
        BattleUnitId? targetId = context.Target.GetUnitId();

        if (targetId is null)
        {
            return BattleItemCalculator.ItemInfo.Fail(BattleCommandError.ItemTargetInvalid);
        }

        BattleUnit target = context.State.GetUnit(targetId.Value);

        if (target.Side != context.User.Side || target.IsAlive || target.IsRevived)
        {
            return BattleItemCalculator.ItemInfo.Fail(BattleCommandError.ItemTargetInvalid);
        }

        int hitpoints = context.EngineContext.Globals.ResurrectionHitpoints;

        return new BattleItemCalculator.ItemInfo([new BattleAttackUnitTarget(target.Id)], [new BattleUnitRevivedEvent(target.Id, hitpoints)], null);
    }

    public BattleItemCalculator.ItemInfo Visit(TemporaryBoostItemEffect effect, Context context)
    {
        return ApplyModifier(context, effect.ModifierId, context.EngineContext.Globals.PotionBoostDurationTurns);
    }

    public BattleItemCalculator.ItemInfo Visit(PermanentBoostItemEffect effect, Context context)
    {
        return ApplyModifier(context, effect.ModifierId, remainingTurns: null);
    }

    public BattleItemCalculator.ItemInfo Visit(SpellItemEffect effect, Context context)
    {
        return BattleItemCalculator.ItemInfo.Fail(BattleCommandError.ItemNotUsable);
    }

    private static BattleItemCalculator.ItemInfo ApplyModifier(Context context, UnitModifierDefinitionId modifierId, int? remainingTurns)
    {
        BattleUnitId? targetId = context.Target.GetUnitId();

        if (targetId is null)
        {
            return BattleItemCalculator.ItemInfo.Fail(BattleCommandError.ItemTargetInvalid);
        }

        BattleUnit target = context.State.GetUnit(targetId.Value);

        if (target.Side != context.User.Side || !target.IsActive())
        {
            return BattleItemCalculator.ItemInfo.Fail(BattleCommandError.ItemTargetInvalid);
        }

        if (target.Modifiers.Any(modifier => modifier.DefinitionId == modifierId))
        {
            return BattleItemCalculator.ItemInfo.Fail(BattleCommandError.UnitModifierAlreadyApplied);
        }

        return new BattleItemCalculator.ItemInfo(
            [new BattleAttackUnitTarget(target.Id)],
            [new BattleUnitModifierAppliedEvent(target.Id, new UnitModifier(modifierId, remainingTurns))],
            null);
    }
}
