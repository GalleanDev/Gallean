using Gallean.Engine;

namespace Gallean.Client;

internal static class LogExtensions
{
    extension(ILog log)
    {
        public void Error(string message, Exception ex)
        {
            log.Write($"{message}{Environment.NewLine}{ex}");
        }

        public void LogWorldCommand<TResult>(IWorldCommand<TResult> command,
            ICommandResult<WorldCommandError, WorldEvent, TResult> commandResult,
            bool dryRun)
        {
            if (commandResult.Failed && !dryRun)
            {
                log.Write($"World command failed. Command={command.GetType().Name}. Error={commandResult.Error.Value}.");
            }
        }

        public void LogBattleCommand<TResult>(IBattleCommand<TResult> command,
            ICommandResult<BattleCommandError, BattleEvent, TResult> commandResult,
            bool dryRun)
        {
            if (commandResult.Failed && !dryRun)
            {
                log.Write($"Battle command failed. Command={command.GetType().Name}. Error={commandResult.Error.Value}.");
            }
        }
    }
}
