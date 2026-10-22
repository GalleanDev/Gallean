using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class BattleAiPlanner
{
    private sealed partial record AttackAction
    {
        private sealed class Scorer : IBattleAttackTargetVisitor<Scorer.Context, int>, IStaticInstance<Scorer>
        {
            public sealed record Context(
                IEngineContext EngineContext,
                BattleAiEstimator Estimator,
                BattleState State,
                BattleUnit Unit,
                UnitAttackDefinition Attack);

            private static readonly UtilityScore.ResponseCurve WeaknessCurve =
                new UtilityScore.ResponseCurve.Polynomial(Slope: MinTargetValue - MaxScore, Exponent: 1, XShift: 0, YShift: MaxScore);

            private static readonly UtilityScore.ResponseCurve PowerCurve =
                new UtilityScore.ResponseCurve.Polynomial(Slope: MaxScore, Exponent: 1, XShift: 0, YShift: MinTargetValue);

            private static readonly UtilityScore.ResponseCurve CountCurve =
                new UtilityScore.ResponseCurve.Root(Slope: MaxScore, Exponent: 2, XShift: 0, YShift: 0);

            public int Visit(BattleAttackUnitTarget target, Context context)
            {
                BattleUnit targetUnit = context.State.GetUnit(target.UnitId);

                return UtilityScore.Combine(GetConsiderations(context, targetUnit));
            }

            public int Visit(BattleAttackTileTarget target, Context context)
            {
                return UtilityScore.Combine([FreePosition(target.Position)]);
            }

            private static IEnumerable<UtilityScore.Consideration> GetConsiderations(Context context, BattleUnit target)
            {
                return context.Attack.Type switch
                {
                    UnitAttackType.Damage => [Weakness(context, target), Lethal(context, target), Unwarded(context, target), Undefended(target)],
                    UnitAttackType.DrainLife => [Weakness(context, target), Lethal(context, target), Unwarded(context, target), Undefended(target)],
                    UnitAttackType.DrainLifeOverflow => [Weakness(context, target), Lethal(context, target), Unwarded(context, target), Undefended(target)],
                    UnitAttackType.Poison => [Weakness(context, target), Unwarded(context, target)],
                    UnitAttackType.Frostbite => [Weakness(context, target), Unwarded(context, target)],
                    UnitAttackType.Blister => [Weakness(context, target), Unwarded(context, target)],
                    UnitAttackType.ReduceDamage => [Weakness(context, target), Unwarded(context, target)],
                    UnitAttackType.ReduceInitiative => [Weakness(context, target), Unwarded(context, target)],
                    UnitAttackType.ReduceLevel => [Weakness(context, target), Unwarded(context, target)],
                    UnitAttackType.ReduceArmor => [Weakness(context, target), Unwarded(context, target)],
                    UnitAttackType.Doppelganger => [Power(context, target)],
                    UnitAttackType.Paralyze => [Power(context, target), Unwarded(context, target), Controllable(target)],
                    UnitAttackType.Petrify => [Power(context, target), Unwarded(context, target), Controllable(target)],
                    UnitAttackType.Fear => [Power(context, target), Unwarded(context, target), Controllable(target)],
                    UnitAttackType.TransformEnemy => [Power(context, target), Unwarded(context, target), Controllable(target), Transformable(target)],
                    UnitAttackType.GiveAdditionalAttack => [Power(context, target)],
                    UnitAttackType.Heal => [Wounded(context, target)],
                    UnitAttackType.Cure => [Afflicted(target)],
                    UnitAttackType.Revive => [Fallen(target)],
                    UnitAttackType.GiveProtection => [Protectable(context, target)],
                    UnitAttackType.IncreaseDamage => [Boostable(context, target)],
                    UnitAttackType.TransformSelf => [UtilityScore.Full],
                    UnitAttackType.Summon => [UtilityScore.None]
                };
            }

            private static UtilityScore.Consideration Weakness(Context context, BattleUnit target)
            {
                int effectiveHitpoints = BattleAiStatCalculator.GetEffectiveHitpoints(context.EngineContext, context.State, target);

                return UtilityScore.Of(Math.Min(effectiveHitpoints, UtilityScore.MaxScore), WeaknessCurve);
            }

            private static UtilityScore.Consideration Power(Context context, BattleUnit target)
            {
                return UtilityScore.Of(context.Estimator.EstimateUnitPower(context.State, target), PowerCurve);
            }

            private static UtilityScore.Consideration Lethal(Context context, BattleUnit target)
            {
                UnitWard? ward = BattleUnitWardCalculator.FindBlockingWard(target, context.Attack);
                UnitAttackStats attackStats = context.EngineContext.UnitStats.GetBattleAttackStats(context.State, context.Unit.Id, context.Attack);
                int hitpoints = BattleAiStatCalculator.GetEffectiveHitpoints(context.EngineContext, context.State, target);
                bool isLethal = ward is null && attackStats.Damage >= hitpoints;

                return UtilityScore.Flag(isLethal, MinTargetValue);
            }

            private static UtilityScore.Consideration Unwarded(Context context, BattleUnit target)
            {
                UnitWard? ward = BattleUnitWardCalculator.FindBlockingWard(target, context.Attack);

                if (ward is not null)
                {
                    return ward.Category switch
                    {
                        UnitWardCategory.Once => UtilityScore.Of(MinTargetValue),
                        UnitWardCategory.Always => UtilityScore.None
                    };
                }
                else
                {
                    return UtilityScore.Full;
                }
            }

            private static UtilityScore.Consideration Undefended(BattleUnit target)
            {
                bool isUndefended = !target.HasStatusEffect<BattleDefendStatusEffect>();

                return UtilityScore.Flag(isUndefended, MinTargetValue);
            }

            private static UtilityScore.Consideration Controllable(BattleUnit target)
            {
                bool isControllable = !target.HasAnyStatusEffect<BattleAbstractDisableEffect, BattleRetreatStatusEffect>();

                return UtilityScore.Flag(isControllable);
            }

            private static UtilityScore.Consideration Transformable(BattleUnit target)
            {
                bool isTransformable = !target.StatusEffects.OfType<BattleTransformStatusEffect>().Any(IsTransformed);

                return UtilityScore.Flag(isTransformable);

                static bool IsTransformed(BattleTransformStatusEffect effect)
                {
                    return effect.Type switch
                    {
                        BattleTransformType.Transform => true,
                        BattleTransformType.TransformSelf => true,
                        BattleTransformType.Doppelganger => true,
                        BattleTransformType.LevelDrain => false
                    };
                }
            }

            private static UtilityScore.Consideration Wounded(Context context, BattleUnit target)
            {
                int maxHitpoints = context.EngineContext.UnitStats.GetBattleUnitStats(context.State, target.Id).MaxHitpoints;

                return UtilityScore.Of(maxHitpoints - target.Hitpoints);
            }

            private static UtilityScore.Consideration Afflicted(BattleUnit target)
            {
                return UtilityScore.Of(target.StatusEffects.Count(effect => effect.IsCurable()), CountCurve);
            }

            private static UtilityScore.Consideration Fallen(BattleUnit target)
            {
                return UtilityScore.Of(target.Level, CountCurve);
            }

            private static UtilityScore.Consideration Protectable(Context context, BattleUnit target)
            {
                IReadOnlyList<BattleUnitWardModifier> wards = BattleUnitWardCalculator.GetGrantableWards(
                    target, context.Unit.Id, context.Attack, context.EngineContext.Definitions);

                return UtilityScore.Of(wards.Count, CountCurve);
            }

            private static UtilityScore.Consideration Boostable(Context context, BattleUnit target)
            {
                if (target.HasStatusEffect<BattleIncreaseDamageStatusEffect>())
                {
                    return UtilityScore.None;
                }
                else
                {
                    UnitDefinition targetDefinition = context.EngineContext.Definitions.GetUnit(target.DefinitionId);

                    if (targetDefinition.HasAnyAttackType(UnitAttackType.Damage, UnitAttackType.DrainLife, UnitAttackType.DrainLifeOverflow))
                    {
                        UnitAttackStats stats = context.EngineContext.UnitStats.GetBattleAttackStats(context.State, target.Id, targetDefinition.Attack);
                        return UtilityScore.Of(stats.Damage);
                    }
                    else
                    {
                        return UtilityScore.None;
                    }
                }
            }

            private static UtilityScore.Consideration FreePosition(GridPosition position)
            {
                return UtilityScore.Flag(position.IsFrontLine, MinTargetValue);
            }
        }

        private const int MinTargetValue = MaxScore / 10;
    }
}
