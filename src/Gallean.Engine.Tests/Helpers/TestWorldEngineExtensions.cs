namespace Gallean.Engine.Tests;

internal static class TestWorldEngineExtensions
{
    extension(WorldEngine engine)
    {
        public TResult CheckCommand<TResult>(
            IWorldCommand<TResult> command,
            IEnumerable<WorldEvent> expectedEvents,
            bool dryRun = false)
        {
            ICommandResult<WorldCommandError, WorldEvent, TResult> result = engine.DispatchCommand(command, dryRun);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Events, Is.EqualTo(expectedEvents));
            Assert.That(result.Data, Is.Not.Null);

            return result.Data;
        }

        public void CheckCommandError<TResult>(
            IWorldCommand<TResult> command,
            WorldCommandError expectedError,
            bool dryRun = false)
        {
            ICommandResult<WorldCommandError, WorldEvent, TResult> result = engine.DispatchCommand(command, dryRun);
            Assert.That(result.Error, Is.EqualTo(expectedError));
        }
    }
}
