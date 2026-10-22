using Gallean.Common;
using Gallean.Engine;
using static Gallean.Client.BattleUnitState;

namespace Gallean.Client;

internal sealed partial class BattleAnimationStateBuilder
{
    private sealed class UnitFeedbackVisitor : IBattleEventVisitor<Empty, (BattleUnitId Unit, Feedback Feedback)?>, IStaticInstance<UnitFeedbackVisitor>
    {
        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitAttackEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleAutoResolveStepEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleEndedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleRoundStartedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleRoundEndedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitAttackHitEvent ev, Empty context)
        {
            if (ev.Damage > 0)
            {
                return (ev.TargetId, new Feedback(FeedbackType.Damage, $"-{ev.Damage}"));
            }
            else
            {
                return null;
            }
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitAttackStepEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitAttackMissedEvent ev, Empty context)
        {
            return (ev.TargetId, new Feedback(FeedbackType.Miss, Tr("Missed")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitAttackBlockedEvent ev, Empty context)
        {
            if (ev.Ward.Category == UnitWardCategory.Always)
            {
                return (ev.TargetId, new Feedback(FeedbackType.Ward, Tr("Immune")));
            }
            else
            {
                return (ev.TargetId, new Feedback(FeedbackType.Ward, Tr("Ward")));
            }
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitAttackedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitDefendedEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Action, Tr("Defend")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitDamagedByEffectEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Damage, $"-{ev.Damage}"));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitDiedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitHealedEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Heal, Tr("Healed")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitRevivedEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Heal, Tr("Revived")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitCuredEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Heal, Tr("Cured")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitAttackGrantedEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Heal, Tr("Extra attack")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleItemUsedEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Action, Tr("Item")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitSummonedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitUnsummonedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitTransformAppliedEvent ev, Empty context)
        {
            string text = ev.Type switch
            {
                BattleTransformType.Transform => Tr("Transformed"),
                BattleTransformType.TransformSelf => Tr("Transformed"),
                BattleTransformType.Doppelganger => Tr("Transformed"),
                BattleTransformType.LevelDrain => Tr("Level drained")
            };

            return (ev.TargetId, new Feedback(FeedbackType.Effect, text));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitTransformRestoredEvent ev, Empty context)
        {
            string text = ev.Type switch
            {
                BattleTransformType.Transform => Tr("Restored"),
                BattleTransformType.TransformSelf => Tr("Restored"),
                BattleTransformType.Doppelganger => Tr("Restored"),
                BattleTransformType.LevelDrain => Tr("Level restored")
            };

            return (ev.UnitId, new Feedback(FeedbackType.Effect, text));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitEffectAppliedEvent ev, Empty context)
        {
            StatusEffect effect = StatusEffect.Create(ev.Effect);
            BattleStatusEffectViewModel viewModel = new(effect);

            return (ev.TargetId, new Feedback(FeedbackType.Effect, viewModel.Name));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitModifierAppliedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitEffectDurationDecreasedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitEffectExpiredEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitExperienceGainedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitUpgradedEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Heal, Tr("Level up!")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitTurnStartedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitTurnSkippedEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Action, Tr("Skip")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitTurnEndingEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitTurnEndedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitWaitedEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Action, Tr("Wait")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitWardConsumedEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitWardGrantedEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Ward, Tr("Protected")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitWardExpiredEvent ev, Empty context)
        {
            return null;
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitRetreatingEvent ev, Empty context)
        {
            return (ev.UnitId, new Feedback(FeedbackType.Action, Tr("Retreat")));
        }

        public (BattleUnitId Unit, Feedback Feedback)? Visit(BattleUnitRetreatedEvent ev, Empty context)
        {
            return null;
        }
    }
}
