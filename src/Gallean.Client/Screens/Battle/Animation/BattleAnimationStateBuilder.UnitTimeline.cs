using static Gallean.Client.BattleUnitState;

namespace Gallean.Client;

internal sealed partial class BattleAnimationStateBuilder
{
    private sealed class UnitTimeline(BattleRenderState.Unit unit)
    {
        public BattleRenderState.Unit Unit { get; } = unit;
        public List<AnimationProperty> Hitpoints { get; } = [];
        public List<BattleAnimationState.BodyEntry> Body { get; } = [];
        public List<BattleAnimationState.FormEntry> Form { get; } = [];
        public List<BattleAnimationState.FeedbackEntry> Feedback { get; } = [];
        public List<BattleAnimationState.MotionEntry> Motion { get; } = [];
        public List<BattleAnimationState.StatusEntry> Status { get; } = [];

        public bool IsAlive => CurrentPresence == Presence.Alive;

        public int CurrentHitpoints
        {
            get
            {
                if (Hitpoints.Count > 0)
                {
                    return (int)Hitpoints[^1].To;
                }
                else
                {
                    return Unit.Hitpoints;
                }
            }
        }

        public BattleSide CurrentFacing
        {
            get
            {
                if (Form.Count > 0)
                {
                    return Form[^1].Facing;
                }
                else
                {
                    return Unit.Facing;
                }
            }
        }

        public Auras CurrentAuras
        {
            get
            {
                if (Form.Count > 0)
                {
                    return Form[^1].Auras;
                }
                else
                {
                    return Unit.Auras;
                }
            }
        }

        public UnitDefinitionId CurrentDefinitionId
        {
            get
            {
                if (Form.Count > 0)
                {
                    return Form[^1].DefinitionId;
                }
                else
                {
                    return Unit.DefinitionId;
                }
            }
        }

        public Presence CurrentPresence
        {
            get
            {
                if (Body.Count > 0)
                {
                    return Body[^1].Presence;
                }
                else
                {
                    return Unit.Presence;
                }
            }
        }

        public TimeSpan HitpointsEnd
        {
            get
            {
                if (Hitpoints.Count > 0)
                {
                    AnimationProperty hitpoints = Hitpoints[^1];

                    return hitpoints.Start + hitpoints.Duration;
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }

        public TimeSpan BodyEnd
        {
            get
            {
                if (Body.Count > 0)
                {
                    BattleAnimationState.BodyEntry body = Body[^1];

                    return body.At + body.Duration;
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }
    }
}
