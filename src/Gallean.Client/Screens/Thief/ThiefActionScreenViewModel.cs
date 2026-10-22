using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class ThiefActionScreenViewModel : AbstractGameScreenViewModel
{
    private readonly WorldStackId stackId;
    private readonly WorldPosition targetPosition;

    public ThiefActionScreenViewModel(
        GameModel gameModel,
        GameViewModel gameViewModel,
        WorldStackId stackId,
        WorldPosition targetPosition)
        : base(gameModel, gameViewModel)
    {
        this.stackId = stackId;
        this.targetPosition = targetPosition;

        RefreshView();
    }

    [ObservableProperty]
    public partial IReadOnlyList<ThiefActionItemViewModel> Actions { get; private set; } = [];

    public async Task SelectActionAsync(ThiefActionItemViewModel item)
    {
        if (IsBusy || !item.Status.IsAvailable)
        {
            return;
        }

        if (CreateAction(item.Type) is { } action)
        {
            await DispatchActionAsync(action);
        }
        else
        {
            Game.OpenScreen(new ThiefStealScreenViewModel(GameModel, Game, stackId, targetPosition, DispatchActionAsync));
        }
    }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    private async Task DispatchActionAsync(WorldThiefAction action)
    {
        if (await TryDispatchWorldActionAsync(new WorldThiefActionCommand(stackId, action)))
        {
            CloseSelf();
        }
    }

    private WorldThiefAction? CreateAction(WorldThiefActionType type)
    {
        return type switch
        {
            WorldThiefActionType.PoisonStack => new WorldThiefPoisonAction(targetPosition),
            WorldThiefActionType.PoisonCity => new WorldThiefPoisonAction(targetPosition),
            WorldThiefActionType.Assassinate => new WorldThiefAssassinateAction(targetPosition),
            WorldThiefActionType.CounterfeitOrders => new WorldThiefCounterfeitOrdersAction(targetPosition),
            WorldThiefActionType.StealGold => new WorldThiefStealGoldAction(targetPosition),
            WorldThiefActionType.Spy => new WorldThiefSpyAction(targetPosition),
            WorldThiefActionType.Duel => new WorldThiefDuelAction(targetPosition),
            WorldThiefActionType.Bribe => new WorldThiefBribeAction(targetPosition),
            WorldThiefActionType.SpyRuin => new WorldThiefSpyRuinAction(targetPosition),
            WorldThiefActionType.RiotCity => new WorldThiefRiotAction(targetPosition),
            WorldThiefActionType.StealItem => null,
            WorldThiefActionType.StealMerchant => null,
            WorldThiefActionType.StealSpell => null,
            WorldThiefActionType.StealMage => null
        };
    }

    private void RefreshView()
    {
        if (!WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack) || stack.OwnerId != ViewPlayerId)
        {
            CloseSelf();

            return;
        }

        Actions = BuildActions(ResolveThiefActions());
    }

    private WorldGetThiefActionsCommand.Result ResolveThiefActions()
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetThiefActionsCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetThiefActionsCommand(stackId, targetPosition));

        if (result.Failed)
        {
            return new WorldGetThiefActionsCommand.Result([]);
        }
        else
        {
            return result.Data;
        }
    }

    private IReadOnlyList<ThiefActionItemViewModel> BuildActions(WorldGetThiefActionsCommand.Result info)
    {
        List<ThiefActionItemViewModel> result = [];

        foreach (WorldGetThiefActionsCommand.ThiefAction action in info.Actions)
        {
            WorldThiefActionDefinition definition = GameModel.Definitions.GetThiefAction(action.Type);

            string name = GameModel.Texts.GetGlobalText(definition.NameTextId).Text;
            string description = GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text;
            TextSpans chanceText = TrSpans("Chance of success: {0:*}", Tr("{0}%", action.Chance));
            string? unavailableReason;

            if (action.Error is { } error)
            {
                unavailableReason = WorldCommandErrorFormatter.Format(error);
            }
            else
            {
                unavailableReason = null;
            }

            result.Add(new ThiefActionItemViewModel(this, action.Type, name, description, chanceText, unavailableReason));
        }

        return [.. result.Order(Comparer<ThiefActionItemViewModel>.Create(CompareItems))];
    }

    private static int CompareItems(ThiefActionItemViewModel left, ThiefActionItemViewModel right)
    {
        bool isLeftUnavailable = !left.Status.IsAvailable;
        bool isRightUnavailable = !right.Status.IsAvailable;

        return isLeftUnavailable.CompareTo(isRightUnavailable);
    }
}
