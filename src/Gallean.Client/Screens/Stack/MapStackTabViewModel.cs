namespace Gallean.Client;

internal sealed class MapStackTabViewModel : StackTabViewModel
{
    private readonly WorldStackId stackId;

    public MapStackTabViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen, WorldStackId stackId)
        : base(gameModel, gameViewModel, screen, leaderBlankslate: null)
    {
        this.stackId = stackId;

        RefreshView();
    }

    protected override void RefreshView()
    {
        if (!WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack) || stack.OwnerId != ViewPlayerId)
        {
            Screen.Close();

            return;
        }

        ShowStack(stack);
    }
}
