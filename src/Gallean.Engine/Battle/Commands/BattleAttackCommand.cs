using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleAttackCommand(BattleUnitId AttackerId, BattleAttackTarget Target) : IBattleCommand<Empty>
{
    public BattleCommandResult Execute(BattleState state, IEngineContext context)
    {
        if (AttackerId != state.CurrentUnitId)
        {
            return BattleCommandResult.Fail(BattleCommandError.NotUnitTurn);
        }

        BattleAttackCalculator.AttackInfo info = BattleAttackCalculator.GetUnitAttackInfo(state, context, AttackerId, Target);

        if (info.Failed)
        {
            return BattleCommandResult.Fail(info.Error.Value);
        }

        IBattleAttackResolver resolver = BattleAttackResolverFactory.Create(info.Attacks[0].Type);

        List<BattleEvent> events =
        [
            new BattleUnitAttackEvent(AttackerId, Target, [.. info.Attacks.Select(definition => definition.Id)]),
            .. info.Events
        ];

        if (resolver.EndsTurn)
        {
            events.Add(new BattleUnitTurnEndingEvent(AttackerId));
        }

        return BattleCommandResult.Success(Empty.Value, events);
    }
}
