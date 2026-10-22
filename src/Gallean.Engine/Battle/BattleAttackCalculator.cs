using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Gallean.Common;

namespace Gallean.Engine;

internal static class BattleAttackCalculator
{
    public sealed record AttackInfo(
        ImmutableArray<UnitAttackDefinition> Attacks,
        IReadOnlyList<BattleAttackTarget> Targets,
        IReadOnlyList<BattleEvent> Events,
        BattleCommandError? Error)
    {
        [MemberNotNullWhen(true, nameof(Error))]
        public bool Failed => Error is not null;

        public static AttackInfo Fail(BattleCommandError error)
        {
            return new AttackInfo([], [], [], error);
        }
    }

    private sealed record TargetInfo(IReadOnlyList<BattleAttackTarget> Targets, BattleCommandError? Error)
    {
        [MemberNotNullWhen(true, nameof(Error))]
        public bool Failed => Error is not null;
    }

    public static AttackInfo GetUnitAttackInfo(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleAttackTarget target)
    {
        BattleUnit attacker = state.GetUnit(attackerId);

        if (!attacker.IsActive())
        {
            return AttackInfo.Fail(BattleCommandError.AttackerDead);
        }

        if (!IsAvailableInTurn(state.CurrentUnitTurn.Type))
        {
            return AttackInfo.Fail(BattleCommandError.ActionNotAvailable);
        }

        BattleUnitId? targetUnitId = target.GetUnitId();
        if (targetUnitId is not null && state.TryGetUnit(targetUnitId.Value) is null)
        {
            return AttackInfo.Fail(BattleCommandError.TargetNotInBattle);
        }

        UnitDefinition attackerDefinition = context.Definitions.GetUnit(attacker.DefinitionId);
        UnitAttackDefinition attack = attackerDefinition.Attack;
        UnitAttackDefinition? alternativeAttack;

        if (attack.AlternativeAttackId is { } alternativeAttackId)
        {
            alternativeAttack = context.Definitions.GetAttack(alternativeAttackId);
        }
        else
        {
            alternativeAttack = null;
        }

        ImmutableArray<UnitAttackDefinition> attacks = GetAttacks(context, attacker, attackerDefinition, attack);
        AttackInfo info = GetAttackInfo(state, context, attackerId, target, attacks);

        if (info.Error is null)
        {
            return info;
        }

        if (alternativeAttack is not null && CanUseAlternativeAttack(state, context, attackerId, attack))
        {
            ImmutableArray<UnitAttackDefinition> alternativeAttacks = GetAttacks(context, attacker, attackerDefinition, alternativeAttack);
            AttackInfo alternativeInfo = GetAttackInfo(state, context, attackerId, target, alternativeAttacks);

            if (alternativeInfo.Error is null)
            {
                return alternativeInfo;
            }
        }

        return info;
    }

    public static AttackInfo GetAttackInfo(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleAttackTarget target,
        UnitAttackDefinition attack)
    {
        return GetAttackInfo(state, context, attackerId, target, [attack]);
    }

    private static AttackInfo GetAttackInfo(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleAttackTarget target,
        ImmutableArray<UnitAttackDefinition> attacks)
    {
        UnitAttackDefinition attack = attacks[0];
        TargetInfo targets = GetTargetInfo(state, attackerId, target, attack);

        if (targets.Failed)
        {
            return AttackInfo.Fail(targets.Error.Value);
        }

        if (attack.Type.GetInfo().TargetType == UnitAttackTargetType.Tile)
        {
            IBattleAttackResolver resolver = BattleAttackResolverFactory.Create(attack.Type);
            UnitAttackStats attackStats = context.UnitStats.GetBattleAttackStats(state, attackerId, attack);
            BattleAttackResult attackResult = resolver.Resolve(state, context, attackerId, target, attack, attackStats);

            if (attackResult.Failed)
            {
                return AttackInfo.Fail(attackResult.Error.Value);
            }

            return new AttackInfo(attacks, targets.Targets, attackResult.Events, null);
        }
        else
        {
            ImmutableArray<UnitAttackDefinitionId> attackIds = [.. attacks.Select(definition => definition.Id)];
            List<BattleUnitId> targetIds = [.. targets.Targets.GetUnitIds()];

            if (attack.Reach != UnitAttackReach.All)
            {
                BattleAttackResult stepResult = ResolveAttackStep(state, context, attackerId, targetIds[0], attackIds, 0);

                if (stepResult.Failed)
                {
                    return AttackInfo.Fail(stepResult.Error.Value);
                }
            }

            List<BattleEvent> events = [];
            int accuracyReductionPerTarget = GetAccuracyReductionPerTarget(state, context, attackerId);
            int accuracyReduction = 0;

            foreach (BattleUnitId targetId in targetIds)
            {
                events.Add(new BattleUnitAttackStepEvent(attackerId, targetId, attackIds, accuracyReduction));

                accuracyReduction += accuracyReductionPerTarget;
            }

            return new AttackInfo(attacks, targets.Targets, events, null);
        }
    }

    private static TargetInfo GetTargetInfo(
        BattleState state,
        BattleUnitId attackerId,
        BattleAttackTarget target,
        UnitAttackDefinition attack)
    {
        UnitAttackTargetType targetType = attack.Type.GetInfo().TargetType;

        if (targetType == UnitAttackTargetType.Tile)
        {
            return new TargetInfo(SummonAttackResolver.Instance.GetTargets(state, attackerId, target, attack), null);
        }

        BattleUnitId? targetUnitId = target.GetUnitId();
        if (targetUnitId is null)
        {
            return new TargetInfo([], BattleCommandError.AttackRequiresUnitTarget);
        }

        BattleUnit attacker = state.GetUnit(attackerId);
        BattleUnit targetUnit = state.GetUnit(targetUnitId.Value);

        BattleSide? targetSide = GetTargetSide(targetType, attacker);

        if (targetSide is not null && targetUnit.Side != targetSide)
        {
            return new TargetInfo([], BattleCommandError.TargetIncorrect);
        }

        if (attack.Reach == UnitAttackReach.All)
        {
            List<BattleAttackTarget> targets = [];

            foreach ((BattleUnit unit, _) in state.GetGrid(targetUnit.Side).GetUnits())
            {
                if (CanTarget(unit, attack))
                {
                    targets.Add(new BattleAttackUnitTarget(unit.Id));
                }
            }

            if (!targets.Contains(target))
            {
                return new TargetInfo([], BattleCommandError.TargetIncorrect);
            }

            return new TargetInfo(targets, null);
        }
        else
        {
            if (targetType == UnitAttackTargetType.Enemy && !ReachCalculator.IsTargetReachable(state, attacker, targetUnit, attack))
            {
                return new TargetInfo([], BattleCommandError.TargetNotReachable);
            }

            return new TargetInfo([target], null);
        }
    }

    public static BattleAttackResult ResolveAttackStep(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnitId targetId,
        ImmutableArray<UnitAttackDefinitionId> attackIds,
        int accuracyReduction)
    {
        List<BattleAttackResult> failed = [];

        for (int i = 0; i < attackIds.Length; i++)
        {
            BattleAttackResult attackResult = ResolveAttack(state, context, attackerId, targetId, attackIds[i], accuracyReduction);

            ImmutableArray<UnitAttackDefinitionId> remaining = attackIds[(i + 1)..];

            if (attackResult.Failed)
            {
                failed.Add(attackResult);
            }
            else if (attackResult.DidHit && !remaining.IsEmpty)
            {
                BattleUnitAttackStepEvent stepEvent = new(attackerId, targetId, remaining, 0);

                return attackResult with { Events = [.. attackResult.Events, stepEvent] };
            }
            else
            {
                return attackResult;
            }
        }

        return failed[0];
    }

    private static BattleAttackResult ResolveAttack(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnitId targetId,
        UnitAttackDefinitionId attackId,
        int accuracyReduction)
    {
        UnitAttackDefinition attack = context.Definitions.GetAttack(attackId);
        IBattleAttackResolver resolver = BattleAttackResolverFactory.Create(attack.Type);

        UnitAttackStats attackStats = context.UnitStats.GetBattleAttackStats(state, attackerId, attack);
        attackStats = attackStats with { Accuracy = Math.Max(0, attackStats.Accuracy - accuracyReduction) };

        return resolver.Resolve(state, context, attackerId, new BattleAttackUnitTarget(targetId), attack, attackStats);
    }

    private static ImmutableArray<UnitAttackDefinition> GetAttacks(
        IEngineContext context,
        BattleUnit attacker,
        UnitDefinition attackerDefinition,
        UnitAttackDefinition attack)
    {
        UnitAttackTargetType targetType = attack.Type.GetInfo().TargetType;
        ImmutableArray<UnitAttackDefinition>.Builder attacks = ImmutableArray.CreateBuilder<UnitAttackDefinition>();

        attacks.Add(attack);

        if (attackerDefinition.SecondaryAttack is not null &&
            attackerDefinition.SecondaryAttack.Type.GetInfo().TargetType == targetType)
        {
            attacks.Add(attackerDefinition.SecondaryAttack);
        }

        foreach (UnitAttackDefinitionId equipmentAttackId in attacker.EquipmentAttackIds)
        {
            UnitAttackDefinition equipmentAttack = context.Definitions.GetAttack(equipmentAttackId);

            if (equipmentAttack.Type.GetInfo().TargetType == targetType)
            {
                attacks.Add(equipmentAttack);
            }
        }

        return attacks.ToImmutable();
    }

    private static BattleSide? GetTargetSide(UnitAttackTargetType targetType, BattleUnit attacker)
    {
        return targetType switch
        {
            UnitAttackTargetType.Enemy => attacker.Side.GetOpposite(),
            UnitAttackTargetType.Ally => attacker.Side,
            UnitAttackTargetType.Any => null,
            UnitAttackTargetType.Tile => attacker.Side
        };
    }

    private static bool CanTarget(BattleUnit unit, UnitAttackDefinition attack)
    {
        UnitAttackTypeInfo attackTypeInfo = attack.Type.GetInfo();

        if (attackTypeInfo.TargetsDead)
        {
            return unit is { IsAlive: false, IsRevived: false };
        }
        else
        {
            return unit.IsActive();
        }
    }

    private static int GetAccuracyReductionPerTarget(BattleState state, IEngineContext context, BattleUnitId attackerId)
    {
        BattleUnit attacker = state.GetUnit(attackerId);
        UnitDefinition unit = context.Definitions.GetUnit(attacker.DefinitionId);

        if (unit is { Category: UnitCategory.Leader, Attack.Reach: UnitAttackReach.All })
        {
            WorldRaceDefinition race = context.Definitions.GetRace(unit.Race);

            if (race.Leaders.Contains(unit.Id))
            {
                return context.Globals.AccuracyReductionPerTarget;
            }
            else
            {
                return 0;
            }
        }
        else
        {
            return 0;
        }
    }

    private static bool CanUseAlternativeAttack(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        UnitAttackDefinition attack)
    {
        IBattleAttackResolver resolver = BattleAttackResolverFactory.Create(attack.Type);

        return resolver.CanUseAlternativeAttack(state, context, attackerId, attack);
    }

    private static bool IsAvailableInTurn(BattleUnitTurnType type)
    {
        return type switch
        {
            BattleUnitTurnType.Normal => true,
            BattleUnitTurnType.SecondAttack => true,
            BattleUnitTurnType.GrantedAttack => true,
            BattleUnitTurnType.Resumed => true,
            BattleUnitTurnType.Doppelganger => true
        };
    }
}
