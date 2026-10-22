using static Gallean.Client.BattleRenderState;

namespace Gallean.Client;

internal static class BattleRenderStateExtensions
{
    extension(BattleRenderState state)
    {
        public BattleRenderState WithFinishedAnimation(BattleAnimationState animation, TimeSpan loopTime)
        {
            TimeSpan end = animation.Sequence.GetTime(animation.Sequence.Duration);
            List<Unit> units = [];

            foreach (Unit unit in state.Units)
            {
                BattleAnimationState.BodyEntry? body = ResolveBody(animation, unit.Id);

                if (body is not null)
                {
                    units.Add(unit with { IdleOffset = end - body.At - loopTime });
                }
                else
                {
                    units.Add(unit);
                }
            }

            return state with { Units = units };
        }
    }

    private static BattleAnimationState.BodyEntry? ResolveBody(BattleAnimationState animation, BattleUnitId unitId)
    {
        foreach (BattleAnimationState.Unit timeline in animation.Units)
        {
            if (timeline.Id == unitId && timeline.Body.Count > 0)
            {
                return timeline.Body[^1];
            }
        }

        return null;
    }
}
