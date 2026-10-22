using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.BattleCommandError, Gallean.Engine.BattleEvent, Gallean.Engine.BattleGetItemInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record BattleGetItemInfoCommand(BattleUnitId UnitId) : IBattleCommand<BattleGetItemInfoCommand.Result>
{
    public sealed record ItemInfo(
        BattleAttackTarget Target,
        ImmutableArray<BattleAttackTarget> Targets,
        BattleCommandError? Error)
    {
        public bool Equals(ItemInfo? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Target == other.Target &&
                   Targets.SequenceEqual(other.Targets) &&
                   Error == other.Error;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(Target);
            hash.AddSequence(Targets);
            hash.Add(Error);

            return hash.ToHashCode();
        }
    }

    public sealed record SlotInfo(LeaderEquipmentSlot Slot, ImmutableArray<ItemInfo> ItemInfos)
    {
        public bool Equals(SlotInfo? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Slot == other.Slot &&
                   ItemInfos.SequenceEqual(other.ItemInfos);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(Slot);
            hash.AddSequence(ItemInfos);

            return hash.ToHashCode();
        }
    }

    public sealed record Result(ImmutableArray<SlotInfo> Slots)
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

            return Slots.SequenceEqual(other.Slots);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Slots);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(BattleState state, IEngineContext context)
    {
        if (UnitId != state.CurrentUnitId)
        {
            return CommandResult.Fail(BattleCommandError.NotUnitTurn);
        }

        BattleItemCalculator.UserInfo info = BattleItemCalculator.GetUserInfo(state, context, UnitId);

        if (info.Failed)
        {
            return CommandResult.Fail(info.Error.Value);
        }

        ImmutableArray<SlotInfo>.Builder slots = ImmutableArray.CreateBuilder<SlotInfo>();

        foreach (BattleItemCalculator.SlotInfo slot in info.Slots)
        {
            ImmutableArray<ItemInfo>.Builder uses = ImmutableArray.CreateBuilder<ItemInfo>();

            foreach (BattleItemCalculator.UseInfo use in slot.Uses)
            {
                uses.Add(new ItemInfo(use.Target, [.. use.Info.Targets], use.Info.Error));
            }

            slots.Add(new SlotInfo(slot.Slot, uses.ToImmutable()));
        }

        return CommandResult.Success(new Result(slots.ToImmutable()), []);
    }
}
