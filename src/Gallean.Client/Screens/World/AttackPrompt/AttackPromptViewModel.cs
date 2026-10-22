using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class AttackPromptViewModel(
    AssetSpriteKey.UnitEventPortrait? portraitKey,
    string targetName,
    bool isTargetKnown,
    Func<Task> confirmAction) : AbstractScreenViewModel
{
    public AssetSpriteKey.UnitEventPortrait? PortraitKey { get; } = portraitKey;
    public TextSpans Message { get; } = TrSpans("The path to the destination leads into battle with {0:*}.", targetName);
    public bool IsTargetKnown { get; } = isTargetKnown;

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        CloseSelf();

        await confirmAction();
    }
}
