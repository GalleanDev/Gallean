namespace Gallean.Engine.Tests;

internal static class TestBattleEngineExtensions
{
    extension(BattleEngine engine)
    {
        public TResult CheckCommand<TResult>(
            IBattleCommand<TResult> command,
            IEnumerable<BattleEvent> expectedEvents,
            bool dryRun = false)
        {
            ICommandResult<BattleCommandError, BattleEvent, TResult> result = engine.DispatchCommand(command, dryRun);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Events, Is.EqualTo(expectedEvents));
            Assert.That(result.Data, Is.Not.Null);

            return result.Data;
        }

        public void CheckCommandError<TResult>(
            IBattleCommand<TResult> command,
            BattleCommandError expectedError,
            bool dryRun = false)
        {
            ICommandResult<BattleCommandError, BattleEvent, TResult> result = engine.DispatchCommand(command, dryRun);
            Assert.That(result.Error, Is.EqualTo(expectedError));
        }
    }
}
