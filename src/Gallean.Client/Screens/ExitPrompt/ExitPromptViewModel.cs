using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class ExitPromptViewModel(GameViewModel game) : AbstractScreenViewModel
{
    public GameViewModel Game { get; } = game;

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    [RelayCommand]
    private async Task ExitAsync()
    {
        await Game.WriteExitSaveAsync();

        Game.ExitCommand.Execute(null);
    }
}
