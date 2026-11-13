using System.Collections.Immutable;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private closed record AbstractStackAction(WorldAiContext Context, int Priority)
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
            WorldPlayerId? DefenderId,
            WorldAiEstimator.PowerEstimate Power,
            int RequiredPowerPercent);

        private sealed class BattleStateVisitor : PathTraversalCalculator.IEncounterVisitor<BattleStateVisitor.Context, BattleStateVisitor.Battle?>
        {
            public sealed record Context(WorldState State, WorldStack Stack, IEngineContext Engine);

            public sealed record Battle(BattleState State, WorldPlayerId? DefenderId);

            public static readonly BattleStateVisitor Instance = new();

            public Battle? Visit(PathTraversalCalculator.Encounter.Bag encounter, Context context)
            {
                return null;
            }

            public Battle? Visit(PathTraversalCalculator.Encounter.StackBattle encounter, Context context)
            {
                BattleState state = WorldCommandHelpers.CreateStackBattleState(context.State, context.Stack, encounter.Stack, context.Engine);

                return new Battle(state, encounter.Stack.OwnerId);
            }

            public Battle? Visit(PathTraversalCalculator.Encounter.FortificationBattle encounter, Context context)
            {
                BattleState state = WorldCommandHelpers.CreateGarrisonBattleState(context.State, context.Stack, encounter.Fortification, context.Engine);

                return new Battle(state, FortificationCalculator.GetOwner(encounter.Fortification));
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

            BattleStateVisitor.Battle? battle = encounter.Accept(BattleStateVisitor.Instance, new BattleStateVisitor.Context(state, stack, aiContext.Engine));

            if (battle is null)
            {
                return null;
            }

            WorldAiEstimator.PowerEstimate power = aiContext.Estimator.EstimatePower(battle.State);
            WorldAiAttackPowerPercent attackPowerPercent = GetAttackPowerPercent(state.Difficulty, aiContext.Engine.Globals);
            int requiredPowerPercent = IsHumanPlayer(state, battle.DefenderId) ? attackPowerPercent.HumanTarget : attackPowerPercent.OtherTarget;

            return new BattleInfo(target, targetPosition, encounter.Position, battle.State, battle.DefenderId, power, requiredPowerPercent);
        }

        private static WorldAiAttackPowerPercent GetAttackPowerPercent(WorldDifficulty difficulty, GlobalVariables globals)
        {
            return difficulty switch
            {
                WorldDifficulty.Easy => globals.AiAttackPowerPercentEasy,
                WorldDifficulty.Average => globals.AiAttackPowerPercentAverage,
                WorldDifficulty.Hard => globals.AiAttackPowerPercentHard,
                WorldDifficulty.VeryHard => globals.AiAttackPowerPercentVeryHard
            };
        }

        private static bool IsHumanPlayer(WorldState state, WorldPlayerId? playerId)
        {
            if (playerId is not null)
            {
                WorldPlayer player = state.Players[playerId.Value];

                return player.Race != WorldRace.Neutral && !player.IsAi;
            }
            else
            {
                return false;
            }
        }

        protected bool CanPickUpBags(WorldState state, WorldStack stack)
        {
            WorldPlayer owner = state.Players[stack.OwnerId];

            if (WorldHelpers.IsRacePlayable(owner.Race))
            {
                (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(Context.Engine.Definitions);

                if (leaderDefinition.Category is UnitCategory.Summon or UnitCategory.Illusion)
                {
                    return false;
                }
                else
                {
                    return leaderDefinition.LeaderCategory switch
                    {
                        UnitLeaderCategory.Fighter => true,
                        UnitLeaderCategory.Explorer => true,
                        UnitLeaderCategory.Mage => true,
                        UnitLeaderCategory.Thief => true,
                        UnitLeaderCategory.Rod => false,
                        null => false
                    };
                }
            }
            else
            {
                return true;
            }
        }
    }
}
