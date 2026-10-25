using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class LeaderUpgradeViewModel : AbstractGameScreenViewModel
{
    private readonly WorldStackId stackId;
    private UnitModifierDefinitionId? selectedModifierId;

    public LeaderUpgradeViewModel(GameModel gameModel, GameViewModel gameViewModel, WorldStackId stackId)
        : base(gameModel, gameViewModel)
    {
        this.stackId = stackId;

        RefreshView();

        RegisterSnapshotGatedCommands(LearnCommand);
    }

    [ObservableProperty]
    public partial IReadOnlyList<LeaderUpgradeItemViewModel> Options { get; private set; } = [];

    [ObservableProperty]
    public partial LeaderUpgradeHeaderViewModel? Header { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LearnCommand))]
    public partial bool HasSelectedUpgrade { get; private set; }

    protected override bool TryClose()
    {
        PostponeCommand.Execute(null);

        return true;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    public void SelectUpgrade(LeaderUpgradeItemViewModel item)
    {
        if (IsBusy)
        {
            return;
        }

        selectedModifierId = item.ModifierId;
        RefreshView();
    }

    [RelayCommand(CanExecute = nameof(CanLearn))]
    private async Task LearnAsync()
    {
        if (selectedModifierId is null)
        {
            return;
        }

        if (!await TryDispatchWorldActionAsync(new WorldSelectLeaderUpgradeCommand(stackId, selectedModifierId.Value)))
        {
            return;
        }

        selectedModifierId = null;

        if (!WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack) || stack.PendingLeaderUpgrades <= 0)
        {
            CloseSelf();
        }
        else
        {
            RefreshView();
        }
    }

    [RelayCommand]
    private async Task PostponeAsync()
    {
        if (WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack) && stack.PendingLeaderUpgrades > 0)
        {
            if (!await TryDispatchWorldActionAsync(new WorldPostponeLeaderUpgradeCommand(stackId)))
            {
                return;
            }
        }

        CloseSelf();
    }

    private bool CanLearn()
    {
        if (IsBusy)
        {
            return false;
        }

        return HasSelectedUpgrade;
    }

    private void RefreshView()
    {
        if (!WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack))
        {
            selectedModifierId = null;
            Header = null;
            Options = [];
            HasSelectedUpgrade = false;

            return;
        }

        (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(GameModel.Definitions);

        WorldPlayer owner = WorldState.Players[stack.OwnerId];
        string name = UnitNameResolver.Resolve(leader, GameModel.Definitions, GameModel.Texts);

        Header = new LeaderUpgradeHeaderViewModel(
            new AssetSpriteKey.UnitFace(leader.DefinitionId),
            Tr("{0} reached level {1}", name, leader.Level),
            TrSpans("Upgrades to select: {0:*}", stack.PendingLeaderUpgrades),
            UnitDetailsViewModel.CreateFromWorld(leader, owner, GameModel.Definitions, GameModel.Texts, GameModel.Stats));

        IReadOnlyList<LeaderUpgradeItemViewModel> options = BuildOptions();

        if (!options.Any(option => option.Status.IsAvailable && option.ModifierId == selectedModifierId))
        {
            selectedModifierId = null;
        }

        foreach (LeaderUpgradeItemViewModel option in options)
        {
            option.IsSelected = option.ModifierId == selectedModifierId;
        }

        Options = options;
        HasSelectedUpgrade = selectedModifierId is not null;
    }

    private IReadOnlyList<LeaderUpgradeItemViewModel> BuildOptions()
    {
        WorldGetLeaderUpgradeInfoCommand.Result info = ResolveLeaderUpgradeInfo();
        IEnumerable<WorldGetLeaderUpgradeInfoCommand.Upgrade> upgrades = info.Upgrades.OrderByDescending(upgrade => upgrade.IsPreferred);

        List<LeaderUpgradeItemViewModel> result = [];

        foreach ((LeaderUpgradeDefinition definition, bool isPreferred, WorldCommandError? error) in upgrades)
        {
            string? unavailableReason;

            if (error is not null)
            {
                unavailableReason = WorldCommandErrorFormatter.Format(error.Value);
            }
            else
            {
                unavailableReason = null;
            }

            result.Add(new LeaderUpgradeItemViewModel(
                this,
                definition.ModifierId,
                UnitModifierIconResolver.Resolve(GameModel.Definitions.GetUnitModifier(definition.ModifierId)),
                GameModel.Texts.GetGlobalText(definition.NameTextId).Text,
                GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text,
                isPreferred,
                unavailableReason));
        }

        return result;
    }

    private WorldGetLeaderUpgradeInfoCommand.Result ResolveLeaderUpgradeInfo()
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetLeaderUpgradeInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetLeaderUpgradeInfoCommand(stackId));

        if (result.Failed)
        {
            return new WorldGetLeaderUpgradeInfoCommand.Result([]);
        }
        else
        {
            return result.Data;
        }
    }
}
