using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class LeaderBlankslateViewModel(GameModel gameModel, GameViewModel gameViewModel, WorldFortificationId fortificationId)
{
    [RelayCommand]
    private void Hire()
    {
        gameViewModel.OpenScreen(new LeaderPurchaseViewModel(gameModel, gameViewModel, fortificationId));
    }
}
