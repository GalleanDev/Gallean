namespace Gallean.Engine;

public sealed class BattleCommandException(BattleCommandError error)
    : Exception($"Command failed with error '{error}'.")
{
    public BattleCommandError Error { get; } = error;
}
