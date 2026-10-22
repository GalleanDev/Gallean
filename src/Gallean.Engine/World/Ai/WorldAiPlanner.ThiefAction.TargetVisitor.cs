using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record ThiefAction
    {
        private sealed class TargetVisitor : IWorldAiTargetVisitor<TargetVisitor.Context, bool>, IStaticInstance<TargetVisitor>
        {
            public readonly record struct Context(WorldState State, WorldPlayerId PlayerId, LordDefinition Lord);

            public static readonly TargetVisitor Instance = new();

            public bool Visit(WorldAiTarget.Stack target, Context context)
            {
                WorldStack stack = context.State.Stacks[target.StackId];

                if (SpyCalculator.SpiesOnPlayer(context.State, context.PlayerId, stack.OwnerId))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public bool Visit(WorldAiTarget.Fortification target, Context context)
            {
                WorldFortification fortification = context.State.Fortifications[target.FortificationId];
                WorldPlayerId? ownerId = FortificationCalculator.GetOwner(fortification);

                if (ownerId is null)
                {
                    return false;
                }

                if (FortificationCalculator.IsRioting(fortification))
                {
                    return false;
                }

                bool canRiot = fortification is WorldCity && ThiefCalculator.IsActionAllowed(WorldThiefActionType.RiotCity, context.Lord);
                bool canSpy = !SpyCalculator.SpiesOnPlayer(context.State, context.PlayerId, ownerId.Value);

                return canRiot || canSpy;
            }

            public bool Visit(WorldAiTarget.Location target, Context context)
            {
                return false;
            }

            public bool Visit(WorldAiTarget.Bag target, Context context)
            {
                return false;
            }
        }
    }
}
