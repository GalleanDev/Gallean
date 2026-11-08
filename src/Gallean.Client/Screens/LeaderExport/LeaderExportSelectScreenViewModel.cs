using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class LeaderExportSelectScreenViewModel : AbstractGameScreenViewModel
{
    private readonly GameMenuViewModel owner;
    private readonly ScenarioExporter scenarioExporter;

    public LeaderExportSelectScreenViewModel(GameModel gameModel, GameViewModel gameViewModel, GameMenuViewModel owner)
        : base(gameModel, gameViewModel)
    {
        this.owner = owner;

        scenarioExporter = new ScenarioExporter(gameModel.Definitions, gameModel.Texts, gameModel.Globals);
        Leaders = BuildLeaders(Snapshot);
        SelectedLeader = ResolveSelection(null);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    public partial LeaderExportCandidateViewModel? SelectedLeader { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial IReadOnlyList<LeaderExportCandidateViewModel> Leaders { get; private set; } = [];

    public bool IsEmpty => Leaders.Count == 0;

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        WorldStackId? selectedStackId = SelectedLeader?.StackId;

        Leaders = BuildLeaders(current);
        SelectedLeader = ResolveSelection(selectedStackId);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLeader))]
    private async Task ExportAsync()
    {
        if (SelectedLeader is null)
        {
            return;
        }

        WorldStack stack = WorldState.Stacks[SelectedLeader.StackId];
        ScenarioLeader scenarioLeader = scenarioExporter.ExportLeader(stack, WorldState.Players[stack.OwnerId]);

        if (await Game.TryExportLeaderAsync(scenarioLeader))
        {
            Game.ShowInfo(IconMoniker.Human, Tr("Leader exported"), SelectedLeader.Name);
            Game.CloseScreen(owner);
        }
    }

    private bool HasSelectedLeader()
    {
        return SelectedLeader is not null;
    }

    private LeaderExportCandidateViewModel? ResolveSelection(WorldStackId? stackId)
    {
        foreach (LeaderExportCandidateViewModel leader in Leaders)
        {
            if (leader.StackId == stackId)
            {
                return leader;
            }
        }

        return Leaders.Count > 0 ? Leaders[0] : null;
    }

    private IReadOnlyList<LeaderExportCandidateViewModel> BuildLeaders(GameSnapshot snapshot)
    {
        WorldState state = snapshot.WorldState;
        ImmutableArray<WorldStack> exportableStacks = scenarioExporter.GetExportableStacks(state, snapshot.ViewPlayerId);
        List<WorldStack> stacks = [.. exportableStacks];

        stacks.Sort(CompareStacks);

        List<LeaderExportCandidateViewModel> result = [];

        foreach (WorldStack stack in stacks)
        {
            (WorldUnit leader, _, _) = stack.GetLeader(GameModel.Definitions);
            List<AssetSpriteKey.ItemIcon> itemIcons = [];

            foreach (WorldItem item in stack.Equipment.Items.Values)
            {
                itemIcons.Add(ItemIconResolver.Resolve(GameModel.Definitions, item.DefinitionId));
            }

            UnitDetailsViewModel details = UnitDetailsViewModel.CreateFromWorld(
                leader, state.Players[stack.OwnerId], GameModel.Definitions, GameModel.Texts, GameModel.Stats);

            result.Add(new LeaderExportCandidateViewModel(
                stack.Id,
                new AssetSpriteKey.UnitPortrait(leader.DefinitionId),
                UnitNameResolver.Resolve(leader, GameModel.Definitions, GameModel.Texts),
                LeaderFormatter.FormatLevel(leader.Level),
                LeaderFormatter.FormatBattlesWon(stack.BattlesWon),
                itemIcons,
                details));
        }

        return result;
    }

    private int CompareStacks(WorldStack left, WorldStack right)
    {
        (WorldUnit leftLeader, _, _) = left.GetLeader(GameModel.Definitions);
        (WorldUnit rightLeader, _, _) = right.GetLeader(GameModel.Definitions);
        int rv;

        rv = rightLeader.Level.CompareTo(leftLeader.Level);
        if (rv != 0)
        {
            return rv;
        }

        rv = right.BattlesWon.CompareTo(left.BattlesWon);
        if (rv != 0)
        {
            return rv;
        }

        rv = string.CompareOrdinal(left.Id.Value, right.Id.Value);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }
}
