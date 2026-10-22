namespace Gallean.Engine;

internal sealed partial class BattleEngine(
    IEngineContext context,
    BattleState initialState,
    IBattleUnitUpgradeResolver upgradeResolver) :
    AbstractEngine<BattleState, BattleCommandError, BattleEvent>(context, initialState, ctx => CreateRules(ctx, upgradeResolver)),
    IBattleEventVisitor<BattleState, BattleState>
{
    private static IReadOnlyList<IEventRule<BattleState, BattleEvent>> CreateRules(
        IEngineContext context,
        IBattleUnitUpgradeResolver upgradeResolver)
    {
        return
        [
            new AttackRule(context),
            new KillRule(context),
            new TurnStartRule(context),
            new EffectExpirationRule(context),
            new ExperienceRule(context),
            new EndBattleRule(context),
            new AwardRule(context, upgradeResolver),
            new AdvanceTurnRule(context),
            new AutoResolveStepRule(context)
        ];
    }

    public override ICommandResult<BattleCommandError, BattleEvent, TResult> DispatchCommand<TResult>(
        ICommand<BattleState, BattleCommandError, BattleEvent, TResult> command,
        bool dryRun = false)
    {
        if (State.Outcome is not null)
        {
            return ICommandResult<BattleCommandError, BattleEvent, TResult>.Fail(BattleCommandError.NoActiveBattle);
        }

        return base.DispatchCommand(command, dryRun);
    }

    protected override BattleState ApplyEvent(BattleEvent ev, BattleState state)
    {
        return ev.Accept(this, state);
    }
}
