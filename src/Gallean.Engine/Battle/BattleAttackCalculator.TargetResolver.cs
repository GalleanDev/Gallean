using Gallean.Common;

namespace Gallean.Engine;

internal static partial class BattleAttackCalculator
{
    private sealed class TargetResolver : IBattleAttackTargetVisitor<TargetResolver.Context, TargetInfo>, IStaticInstance<TargetResolver>
    {
        public sealed record Context(BattleState State, BattleUnit Attacker, UnitAttackDefinition Attack);

        public TargetInfo Visit(BattleAttackUnitTarget target, Context context)
        {
            BattleUnit targetUnit = context.State.GetUnit(target.UnitId);

            if (!IsTargetSide(context, targetUnit.Side))
            {
                return new TargetInfo([], BattleCommandError.TargetIncorrect);
            }

            if (context.Attack.Reach == UnitAttackReach.All)
            {
                List<BattleAttackTarget> targets = [.. EnumerateSideTargets(context, targetUnit.Side)];

                if (!targets.Contains(target))
                {
                    return new TargetInfo([], BattleCommandError.TargetIncorrect);
                }

                return new TargetInfo(targets, null);
            }
            else
            {
                UnitAttackTypeInfo attackTypeInfo = context.Attack.Type.GetInfo();

                if (attackTypeInfo.TargetType == UnitAttackTargetType.Enemy)
                {
                    if (!ReachCalculator.IsTargetReachable(context.State, context.Attacker, targetUnit, context.Attack))
                    {
                        return new TargetInfo([], BattleCommandError.TargetNotReachable);
                    }
                }

                return new TargetInfo([target], null);
            }
        }

        public TargetInfo Visit(BattleAttackTileTarget target, Context context)
        {
            if (context.Attack.Reach != UnitAttackReach.All)
            {
                return new TargetInfo([], BattleCommandError.AttackRequiresUnitTarget);
            }

            if (!IsTargetSide(context, target.Side))
            {
                return new TargetInfo([], BattleCommandError.TargetIncorrect);
            }

            List<BattleAttackTarget> targets = [.. EnumerateSideTargets(context, target.Side)];

            if (targets.Count <= 0)
            {
                return new TargetInfo([], BattleCommandError.TargetIncorrect);
            }

            return new TargetInfo(targets, null);
        }

        private static bool IsTargetSide(Context context, BattleSide side)
        {
            UnitAttackTypeInfo attackTypeInfo = context.Attack.Type.GetInfo();

            return attackTypeInfo.TargetType switch
            {
                UnitAttackTargetType.Enemy => side != context.Attacker.Side,
                UnitAttackTargetType.Ally => side == context.Attacker.Side,
                UnitAttackTargetType.Any => true,
                UnitAttackTargetType.Tile => side == context.Attacker.Side
            };
        }

        private static IEnumerable<BattleAttackTarget> EnumerateSideTargets(Context context, BattleSide side)
        {
            foreach ((BattleUnit unit, _) in context.State.GetGrid(side).GetUnits())
            {
                if (CanTarget(unit, context.Attack))
                {
                    yield return new BattleAttackUnitTarget(unit.Id);
                }
            }
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
    }
}
