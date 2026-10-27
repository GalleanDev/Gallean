using System.Collections.Immutable;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private abstract record AbstractStackAction(WorldAiContext Context, int Priority)
        : AbstractAction(Context, Priority), IWorldStackOrderVisitor<AbstractStackAction.ActionContext, WorldAiActionPlan?>
    {
        internal readonly record struct ActionContext(
            WorldState State,
            WorldStack Stack,
            WorldAiMemory Memory,
            ImmutableHashSet<WorldAiTarget> ObjectiveTargets,
            ImmutableHashSet<WorldAiTarget> BlockedTargets,
            bool IsStackVital);

        protected sealed record BattleInfo(
            WorldAiTarget Target,
            WorldPosition TargetPosition,
            WorldPosition BattlePosition,
            BattleState BattleState,
            WorldAiEstimator.PowerEstimate Power);

        private sealed class BattleStateVisitor : PathTraversalCalculator.IEncounterVisitor<BattleStateVisitor.Context, BattleState?>
        {
            public sealed record Context(WorldState State, WorldStack Stack, IEngineContext Engine);

            public static readonly BattleStateVisitor Instance = new();

            public BattleState? Visit(PathTraversalCalculator.Encounter.Bag encounter, Context context)
            {
                return null;
            }

            public BattleState? Visit(PathTraversalCalculator.Encounter.StackBattle encounter, Context context)
            {
                return WorldCommandHelpers.CreateStackBattleState(context.State, context.Stack, encounter.Stack, context.Engine);
            }

            public BattleState? Visit(PathTraversalCalculator.Encounter.FortificationBattle encounter, Context context)
            {
                return WorldCommandHelpers.CreateGarrisonBattleState(context.State, context.Stack, encounter.Fortification, context.Engine);
            }
        }

        public abstract WorldAiActionPlan? Visit(WorldStackOrder.Normal order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.Stand order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.Guard order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.AttackStack order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.DefendStack order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.SecureCity order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.Roam order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.Explore order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.MoveToLocation order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.DefendLocation order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.Berserk order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.Assist order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.Steal order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.DefendCity order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.Heal order, ActionContext context);
        public abstract WorldAiActionPlan? Visit(WorldStackOrder.Reinforce order, ActionContext context);

        public WorldAiActionPlan? Plan(
            WorldState state,
            WorldStack stack,
            WorldStackOrder order,
            WorldAiMemory memory,
            ImmutableHashSet<WorldAiTarget> objectiveTargets,
            ImmutableHashSet<WorldAiTarget> blockedTargets,
            bool isVital)
        {
            return order.Accept(this, new ActionContext(state, stack, memory, objectiveTargets, blockedTargets, isVital));
        }

        protected static BattleInfo? GetBattleInfo(
            WorldAiContext aiContext,
            WorldState state,
            WorldStack stack,
            WorldAiTarget target,
            WorldPosition targetPosition,
            PathTraversalCalculator.Encounter? encounter)
        {
            (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(aiContext.Engine.Definitions);

            if (encounter is null || leaderDefinition.Category == UnitCategory.Illusion)
            {
                return null;
            }

            BattleState? battleState = encounter.Accept(BattleStateVisitor.Instance, new BattleStateVisitor.Context(state, stack, aiContext.Engine));

            if (battleState is null)
            {
                return null;
            }

            WorldAiEstimator.PowerEstimate power = aiContext.Estimator.EstimatePower(battleState);

            return new BattleInfo(target, targetPosition, encounter.Position, battleState, power);
        }
    }
}
