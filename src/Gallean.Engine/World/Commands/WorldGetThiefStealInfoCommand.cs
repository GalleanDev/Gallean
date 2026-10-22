using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetThiefStealInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetThiefStealInfoCommand(
    WorldStackId StackId,
    WorldPosition TargetPosition) : IWorldCommand<WorldGetThiefStealInfoCommand.Result>
{
    public sealed record Item(
        WorldThiefStealItemAction Action,
        WorldCommandError? Error);

    public sealed record Spell(
        WorldThiefStealSpellAction Action,
        WorldCommandError? Error);

    public sealed record Result(ImmutableArray<Item> Items, ImmutableArray<Spell> Spells)
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

            return Items.SequenceEqual(other.Items) &&
                   Spells.SequenceEqual(other.Spells);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Items);
            hash.AddSequence(Spells);

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
            return CommandResult.Success(new Result([], []), []);
        }

        if (!ThiefCalculator.CanReachTarget(stack, target))
        {
            return CommandResult.Success(new Result([], []), []);
        }

        ImmutableArray<ItemDefinitionId> itemIds = ThiefCalculator.GetStealableItems(target);
        ImmutableArray<Item>.Builder items = ImmutableArray.CreateBuilder<Item>(itemIds.Length);

        foreach (ItemDefinitionId itemId in itemIds)
        {
            WorldThiefStealItemAction action = new(TargetPosition, itemId);
            WorldCommandResult actionResult = new WorldThiefActionCommand(StackId, action).Execute(state, context);

            items.Add(new Item(action, actionResult.Error));
        }

        ImmutableArray<WorldSpellDefinitionId> spellIds = ThiefCalculator.GetStealableSpells(target, state);
        ImmutableArray<Spell>.Builder spells = ImmutableArray.CreateBuilder<Spell>(spellIds.Length);

        foreach (WorldSpellDefinitionId spellId in spellIds)
        {
            WorldThiefStealSpellAction action = new(TargetPosition, spellId);
            WorldCommandResult actionResult = new WorldThiefActionCommand(StackId, action).Execute(state, context);

            spells.Add(new Spell(action, actionResult.Error));
        }

        return CommandResult.Success(new Result(items.ToImmutable(), spells.ToImmutable()), []);
    }
}
