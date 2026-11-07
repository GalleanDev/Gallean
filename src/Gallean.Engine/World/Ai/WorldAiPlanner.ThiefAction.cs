using static Gallean.Engine.WorldAiTargetEnumerator;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record ThiefAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
    {
        private const int MoveTurnLimit = 10;

        private sealed record Approach(WorldState State, WorldStack Thief, ThiefTarget Target, WorldPosition Position);

        private sealed record Candidate(WorldAiPathfinding.TargetPath TargetPath, WorldThiefAction Action, int Score);

        public override WorldAiActionPlan? Visit(WorldStackOrder.Normal order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Stand order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Guard order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.AttackStack order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendStack order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.SecureCity order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Roam order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Explore order, ActionContext context)
        {
            return PlanThiefAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.MoveToLocation order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendLocation order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Berserk order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Assist order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Steal order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendCity order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Heal order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Reinforce order, ActionContext context)
        {
            return null;
        }

        private WorldAiActionPlan? PlanThiefAction(ActionContext context)
        {
            WorldState state = context.State;
            WorldStack stack = context.Stack;

            if (!ThiefCalculator.IsThief(stack, Context.Engine.Definitions))
            {
                return null;
            }

            if (stack.MovementPoints <= 0)
            {
                return null;
            }

            WorldPathMap? pathMap = Context.Pathfinding.GetPathMap(state, stack);

            if (pathMap is null)
            {
                return null;
            }

            WorldAiStackMemory stackMemory = context.Memory.GetStackMemory(stack);
            IEnumerable<WorldAiTarget> targets = Enumerate(state, stack.OwnerId, TargetType.Enemy | TargetType.Ruin | TargetType.Site, area: null);
            IReadOnlyList<WorldAiPathfinding.TargetPath> paths = Context.Pathfinding.GetTargetPaths(state, pathMap, targets);
            Candidate? best = FindBestCandidate(context, paths, stackMemory.Target);

            if (best is null)
            {
                return null;
            }

            WorldAiMemory memory = context.Memory.WithStackMemory(stack.Id, stackMemory with { Target = best.TargetPath.Target });

            if (stack.Position.IsAdjacentTo(best.TargetPath.Position))
            {
                return MakePlan(best.Score, new WorldThiefActionCommand(stack.Id, best.Action), memory);
            }
            else
            {
                return PlanMoveToward(context, pathMap, best, memory);
            }
        }

        private Candidate? FindBestCandidate(ActionContext context, IReadOnlyList<WorldAiPathfinding.TargetPath> paths, WorldAiTarget? committedTarget)
        {
            Candidate? best = null;

            foreach (WorldAiPathfinding.TargetPath path in paths)
            {
                Candidate? candidate = FindBestAction(context, path);

                if (candidate is not null)
                {
                    if (path.Target == committedTarget)
                    {
                        return candidate;
                    }

                    if (best is null || candidate.Score > best.Score)
                    {
                        best = candidate;
                    }
                }
            }

            return best;
        }

        private Candidate? FindBestAction(ActionContext context, WorldAiPathfinding.TargetPath targetPath)
        {
            if (targetPath.Path.Segments.IsEmpty)
            {
                return null;
            }

            WorldPosition position = targetPath.Path.Segments[^1].Start;
            WorldStack thief = context.Stack with { Position = position };
            WorldState state = context.State with { Stacks = context.State.Stacks.SetItem(thief.Id, thief) };

            ThiefTarget? target = ThiefCalculator.ResolveTarget(state, Context.Engine, thief.OwnerId, targetPath.Position);
            if (target is null)
            {
                return null;
            }

            Approach approach = new(state, thief, target, targetPath.Position);
            WorldPlayer player = state.Players[thief.OwnerId];
            WorldAiAttitudeDefinition attitude = Context.Engine.Definitions.GetAiAttitude(player.AiAttitude);
            int interest = targetPath.Target.Accept(new WorldAiThiefTargetInterestVisitor(attitude), new WorldAiTargetInterestVisitor.Context(state, thief.OwnerId));
            int priority = targetPath.Target.Accept(new WorldAiTargetPriorityVisitor(), new WorldAiTargetPriorityVisitor.Context(state));
            StackStats stackStats = Context.Engine.UnitStats.GetWorldStackStats(thief);

            List<UtilityScore.Consideration> considerations =
            [
                Considerations.TargetInterest(interest),
                Considerations.TargetPriority(priority),
                Considerations.TargetMoveCost(targetPath.Path.Cost, stackStats.MaxMovementPoints * MoveTurnLimit),
                Considerations.ScenarioBlockedTarget(context, targetPath.Target)
            ];

            Candidate? best = null;

            foreach (WorldThiefActionType type in ThiefCalculator.GetActionTypes(target))
            {
                WorldThiefActionDefinition definition = Context.Engine.Definitions.GetThiefAction(type);
                int chance = ThiefCalculator.GetChance(definition, target.Protection);

                foreach ((WorldThiefAction action, UtilityScore.Consideration value) in CreateActions(type, approach))
                {
                    WorldThiefActionCommand command = new(thief.Id, action);
                    WorldCommandResult commandResult = command.Execute(state, Context.Engine);

                    if (!commandResult.Failed)
                    {
                        int score = UtilityScore.Combine(
                        [
                            value,
                            Considerations.SuccessChance(chance),
                            .. considerations
                        ]);

                        if (score > 0 && (best is null || score > best.Score))
                        {
                            best = new Candidate(targetPath, action, score);
                        }
                    }
                }
            }

            return best;
        }

        private IEnumerable<(WorldThiefAction Action, UtilityScore.Consideration Value)> CreateActions(WorldThiefActionType type, Approach approach)
        {
            WorldPosition position = approach.Position;

            return type switch
            {
                WorldThiefActionType.PoisonStack => [(new WorldThiefPoisonAction(position), Considerations.Poison())],
                WorldThiefActionType.Spy => [(new WorldThiefSpyAction(position), Considerations.Spy(approach))],
                WorldThiefActionType.StealItem => CreateStealItemActions(approach),
                WorldThiefActionType.Assassinate => [(new WorldThiefAssassinateAction(position), Considerations.Assassinate())],
                WorldThiefActionType.CounterfeitOrders => [(new WorldThiefCounterfeitOrdersAction(position), Considerations.CounterfeitOrders(approach, Context))],
                WorldThiefActionType.Duel => [(new WorldThiefDuelAction(position), Considerations.Duel(approach, Context))],
                WorldThiefActionType.PoisonCity => [(new WorldThiefPoisonAction(position), Considerations.Poison())],
                WorldThiefActionType.StealSpell => CreateStealSpellActions(approach),
                WorldThiefActionType.Bribe => [(new WorldThiefBribeAction(position), Considerations.Bribe(approach, Context))],
                WorldThiefActionType.StealGold => [(new WorldThiefStealGoldAction(position), Considerations.StealGold(approach, Context))],
                WorldThiefActionType.StealMerchant => CreateStealItemActions(approach),
                WorldThiefActionType.StealMage => CreateStealSpellActions(approach),
                WorldThiefActionType.SpyRuin => [(new WorldThiefSpyRuinAction(position), Considerations.SpyRuin())],
                WorldThiefActionType.RiotCity => [(new WorldThiefRiotAction(position), Considerations.Riot(approach))]
            };
        }

        private IEnumerable<(WorldThiefAction Action, UtilityScore.Consideration Value)> CreateStealItemActions(Approach approach)
        {
            HashSet<ItemDefinitionId> itemIds = [];

            foreach (ItemDefinitionId itemId in ThiefCalculator.GetStealableItems(approach.Target))
            {
                ItemDefinition item = Context.Engine.Definitions.GetItem(itemId);

                if (CanStealItem(item.Category))
                {
                    if (itemIds.Add(itemId))
                    {
                        yield return (new WorldThiefStealItemAction(approach.Position, itemId), Considerations.StealItem(itemId, Context));
                    }
                }
            }
        }

        private static bool CanStealItem(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Armor => true,
                ItemCategory.Jewel => true,
                ItemCategory.Weapon => true,
                ItemCategory.Banner => true,
                ItemCategory.PotionTemporaryBoost => true,
                ItemCategory.PotionHeal => true,
                ItemCategory.PotionRevive => true,
                ItemCategory.PotionPermanentBoost => true,
                ItemCategory.Scroll => true,
                ItemCategory.Wand => true,
                ItemCategory.Valuable => true,
                ItemCategory.Orb => true,
                ItemCategory.Talisman => true,
                ItemCategory.TravelItem => true,
                ItemCategory.Special => false
            };
        }

        private IEnumerable<(WorldThiefAction Action, UtilityScore.Consideration Value)> CreateStealSpellActions(Approach approach)
        {
            foreach (WorldSpellDefinitionId spellId in ThiefCalculator.GetStealableSpells(approach.Target, approach.State))
            {
                yield return (new WorldThiefStealSpellAction(approach.Position, spellId), Considerations.StealSpell(spellId, Context));
            }
        }

        private WorldAiActionPlan? PlanMoveToward(ActionContext context, WorldPathMap pathMap, Candidate candidate, WorldAiMemory memory)
        {
            WorldStack stack = context.Stack;
            WorldPosition? movePosition = Context.Pathfinding.FindMoveTowardCell(context.State, stack, pathMap, candidate.TargetPath.Position);

            if (movePosition is not null)
            {
                return MakePlan(candidate.Score, new WorldAiMoveStackCommand(stack.Id, movePosition.Value), memory);
            }
            else
            {
                return null;
            }
        }
    }
}
