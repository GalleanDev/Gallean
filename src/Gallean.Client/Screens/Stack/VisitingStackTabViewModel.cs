using Gallean.Engine;

namespace Gallean.Client;

internal sealed class VisitingStackTabViewModel : StackTabViewModel
{
    private readonly WorldFortificationId fortificationId;

    public VisitingStackTabViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen, WorldFortificationId fortificationId)
        : base(gameModel, gameViewModel, screen, new LeaderBlankslateViewModel(gameModel, gameViewModel, fortificationId))
    {
        this.fortificationId = fortificationId;

        RefreshView();
    }

    protected override void RefreshView()
    {
        if (!WorldState.Fortifications.TryGetValue(fortificationId, out WorldFortification? fortification))
        {
            Screen.Close();

            return;
        }

        WorldPlan plan = WorldPlan.For(WorldState);
        WorldStack? stack = plan.GetStackAt(fortification.Entrance);

        if (stack is null || stack.OwnerId != ViewPlayerId)
        {
            ShowNoStack();

            return;
        }

        ShowStack(stack);
    }
}
