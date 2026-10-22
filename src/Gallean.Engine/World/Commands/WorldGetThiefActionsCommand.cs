using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetThiefActionsCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetThiefActionsCommand(
    WorldStackId StackId,
    WorldPosition TargetPosition) : IWorldCommand<WorldGetThiefActionsCommand.Result>
{
    public sealed record ThiefAction(
        WorldThiefActionType Type,
        int Chance,
        WorldCommandError? Error);

    public sealed record Result(ImmutableArray<ThiefAction> Actions)
    {
        public bool Equals(Result? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Actions.SequenceEqual(other.Actions);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Actions);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayerId currentPlayer = state.TurnInfo.CurrentPlayerId;

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return CommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!ThiefCalculator.IsThief(stack, context.Definitions))
        {
            return CommandResult.Fail(WorldCommandError.ThiefLeaderRequired);
        }

        ThiefTarget? target = ThiefCalculator.ResolveTarget(state, context, currentPlayer, TargetPosition);

        if (target is null)
        {
            return CommandResult.Success(new Result([]), []);
        }

        if (!ThiefCalculator.CanReachTarget(stack, target))
        {
            return CommandResult.Success(new Result([]), []);
        }

        ImmutableArray<WorldThiefActionType> types = ThiefCalculator.GetActionTypes(target);
        ImmutableArray<ThiefAction>.Builder actions = ImmutableArray.CreateBuilder<ThiefAction>(types.Length);

        foreach (WorldThiefActionType type in types)
        {
            WorldThiefActionDefinition definition = context.Definitions.GetThiefAction(type);
            int chance = ThiefCalculator.GetChance(definition, target.Protection);
            WorldThiefAction? action = CreateAction(type);
            WorldCommandError? error;

            if (action is not null)
            {
                WorldCommandResult actionResult = new WorldThiefActionCommand(StackId, action).Execute(state, context);

                error = actionResult.Error;
            }
            else
            {
                error = GetStealError(state, context, target);
            }

            actions.Add(new ThiefAction(type, chance, error));
        }

        return CommandResult.Success(new Result(actions.ToImmutable()), []);
    }

    private WorldCommandError? GetStealError(WorldState state, IEngineContext context, ThiefTarget target)
    {
        ImmutableArray<ItemDefinitionId> itemIds = ThiefCalculator.GetStealableItems(target);
        ImmutableArray<WorldSpellDefinitionId> spellIds = ThiefCalculator.GetStealableSpells(target, state);
        List<WorldThiefAction> stealActions = [];

        foreach (ItemDefinitionId itemId in itemIds)
        {
            stealActions.Add(new WorldThiefStealItemAction(TargetPosition, itemId));
        }

        foreach (WorldSpellDefinitionId spellId in spellIds)
        {
            stealActions.Add(new WorldThiefStealSpellAction(TargetPosition, spellId));
        }

        List<WorldCommandError?> errors = [];

        foreach (WorldThiefAction stealAction in stealActions)
        {
            WorldCommandResult stealResult = new WorldThiefActionCommand(StackId, stealAction).Execute(state, context);

            errors.Add(stealResult.Error);
        }

        if (errors.Count == 0)
        {
            return WorldCommandError.ThiefNothingToSteal;
        }
        else if (errors.Contains(null))
        {
            return null;
        }
        else
        {
            return errors[0];
        }
    }

    private WorldThiefAction? CreateAction(WorldThiefActionType type)
    {
        return type switch
        {
            WorldThiefActionType.PoisonStack => new WorldThiefPoisonAction(TargetPosition),
            WorldThiefActionType.PoisonCity => new WorldThiefPoisonAction(TargetPosition),
            WorldThiefActionType.Assassinate => new WorldThiefAssassinateAction(TargetPosition),
            WorldThiefActionType.CounterfeitOrders => new WorldThiefCounterfeitOrdersAction(TargetPosition),
            WorldThiefActionType.StealGold => new WorldThiefStealGoldAction(TargetPosition),
            WorldThiefActionType.Spy => new WorldThiefSpyAction(TargetPosition),
            WorldThiefActionType.Duel => new WorldThiefDuelAction(TargetPosition),
            WorldThiefActionType.Bribe => new WorldThiefBribeAction(TargetPosition),
            WorldThiefActionType.SpyRuin => new WorldThiefSpyRuinAction(TargetPosition),
            WorldThiefActionType.RiotCity => new WorldThiefRiotAction(TargetPosition),
            WorldThiefActionType.StealItem => null,
            WorldThiefActionType.StealMerchant => null,
            WorldThiefActionType.StealSpell => null,
            WorldThiefActionType.StealMage => null
        };
    }
}
