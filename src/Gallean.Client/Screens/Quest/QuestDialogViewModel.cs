namespace Gallean.Client;

internal sealed record QuestDialogViewModel(int TurnNumber, IReadOnlyList<QuestMessageViewModel> Messages)
{
    public string TurnText => Tr("Turn {0}", TurnNumber);
}
