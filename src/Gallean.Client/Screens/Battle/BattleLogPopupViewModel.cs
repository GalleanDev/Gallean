namespace Gallean.Client;

internal sealed partial class BattleLogPopupViewModel(IReadOnlyList<BattleLogEntry> entries) : AbstractScreenViewModel
{
    public IReadOnlyList<BattleLogEntry> Entries { get; } = entries;

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }
}
