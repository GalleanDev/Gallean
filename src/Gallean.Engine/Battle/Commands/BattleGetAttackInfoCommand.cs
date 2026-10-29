using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.BattleCommandError, Gallean.Engine.BattleEvent, Gallean.Engine.BattleGetAttackInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record BattleGetAttackInfoCommand(BattleUnitId AttackerId) : IBattleCommand<BattleGetAttackInfoCommand.Result>
{
    public sealed record AttackInfo(
        BattleAttackTarget Target,
        ImmutableArray<BattleAttackTarget> Targets,
        BattleCommandError? Error)
    {
        public bool Equals(AttackInfo? other)
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

    public sealed record Result(ImmutableArray<AttackInfo> AttackInfos)
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

            return AttackInfos.SequenceEqual(other.AttackInfos);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(AttackInfos);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(BattleState state, IEngineContext context)
    {
        if (AttackerId != state.CurrentUnitId)
        {
            return CommandResult.Fail(BattleCommandError.NotUnitTurn);
        }

        ImmutableArray<AttackInfo>.Builder attacks = ImmutableArray.CreateBuilder<AttackInfo>();

        foreach (BattleAttackTarget target in EnumerateAttackTargets(state))
        {
            BattleAttackCalculator.AttackInfo info = BattleAttackCalculator.GetUnitAttackInfo(state, context, AttackerId, target);

            attacks.Add(new AttackInfo(target, [.. info.Targets], info.Error));
        }

        return CommandResult.Success(new Result(attacks.ToImmutable()), []);
    }

    private static IEnumerable<BattleAttackTarget> EnumerateAttackTargets(BattleState state)
    {
        foreach (BattleUnitId id in state.GetAllUnitIds())
        {
            yield return new BattleAttackUnitTarget(id);
        }

        foreach (BattleSide side in Enum.GetValues<BattleSide>())
        {
            foreach (GridPosition position in GridPosition.All)
            {
                yield return new BattleAttackTileTarget(side, position);
            }
        }
    }
}
