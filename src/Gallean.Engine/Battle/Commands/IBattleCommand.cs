namespace Gallean.Engine;

public interface IBattleCommand<TResult> : ICommand<BattleState, BattleCommandError, BattleEvent, TResult>;
