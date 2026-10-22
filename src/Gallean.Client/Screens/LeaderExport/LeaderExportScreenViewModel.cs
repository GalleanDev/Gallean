using System.Collections.Immutable;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class LeaderExportScreenViewModel : AbstractGameScreenViewModel
{
    private readonly ScenarioId nextScenarioId;
    private readonly ScenarioExporter scenarioExporter;
    private readonly List<WorldItem> selectedItems = [];

    public LeaderExportScreenViewModel(GameModel gameModel, GameViewModel gameViewModel)
        : base(gameModel, gameViewModel)
    {
        Debug.Assert(gameModel.Scenario.Definition.NextScenarioId is not null);

        nextScenarioId = gameModel.Scenario.Definition.NextScenarioId.Value;
        scenarioExporter = new ScenarioExporter(gameModel.Definitions, gameModel.Globals);
        Leaders = BuildLeaders();
        SelectedLeader = Leaders.Count > 0 ? Leaders[0] : null;

        RefreshItems();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    public partial LeaderExportCandidateViewModel? SelectedLeader { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<LeaderExportItemViewModel> Items { get; private set; } = [];

    [ObservableProperty]
    public partial TextSpans SelectedItemsText { get; private set; } = TextSpans.Empty;

    public IReadOnlyList<LeaderExportCandidateViewModel> Leaders { get; }

    public string Instruction => Tr("Select a leader and up to {0} items to bring to the next quest.", scenarioExporter.ItemLimit);

    public static bool CanExportLeader(GameModel gameModel)
    {
        GameSnapshot snapshot = gameModel.Snapshot;
        ScenarioExporter exporter = new(gameModel.Definitions, gameModel.Globals);
        ImmutableArray<WorldStack> stacks = exporter.GetExportableStacks(snapshot.WorldState, snapshot.ViewPlayerId);

        return stacks.Length > 0;
    }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
    }

    public void AddItem(LeaderExportItemViewModel row)
    {
        foreach (WorldItem item in row.Items)
        {
            if (!selectedItems.Contains(item))
            {
                selectedItems.Add(item);
                break;
            }
        }

        RefreshItems();
    }

    public void RemoveItem(LeaderExportItemViewModel row)
    {
        for (int index = selectedItems.Count - 1; index >= 0; index--)
        {
            if (row.Items.Contains(selectedItems[index]))
            {
                selectedItems.RemoveAt(index);
                break;
            }
        }

        RefreshItems();
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        if (SelectedLeader is null)
        {
            return;
        }

        WorldStack stack = WorldState.Stacks[SelectedLeader.StackId];
        ScenarioLeader scenarioLeader = scenarioExporter.ExportLeader(stack, selectedItems);

        if (selectedItems.Count < scenarioExporter.ItemLimit && HasUnselectedItems())
        {
            Game.OpenScreen(new ConfirmPromptViewModel(
                Tr("Export leader"),
                FormatUnderLimitPrompt(selectedItems.Count),
                Tr("Continue"),
                ConfirmPromptType.Primary,
                () => Game.StartNextScenarioAsync(GameModel, nextScenarioId, scenarioLeader)));
        }
        else
        {
            await Game.StartNextScenarioAsync(GameModel, nextScenarioId, scenarioLeader);
        }
    }

    private bool HasUnselectedItems()
    {
        foreach (LeaderExportItemViewModel row in Items)
        {
            if (row.Selected < row.Items.Count)
            {
                return true;
            }
        }

        return false;
    }

    private bool CanExport()
    {
        return SelectedLeader is not null;
    }

    private static TextSpans FormatUnderLimitPrompt(int selectedCount)
    {
        return selectedCount switch
        {
            0 => TrSpans("Continue without any items?"),
            1 => TrSpans("Continue with only one item?"),
            _ => TrSpans("Continue with only {0} items?", selectedCount)
        };
    }

    private static string FormatLeaderSummary(int level, int battlesWon)
    {
        JoinStringBuilder sb = new(" · ");

        sb.Add(Tr("Level {0}", level));

        if (battlesWon == 1)
        {
            sb.Add(Tr("1 battle won"));
        }
        else
        {
            sb.Add(Tr("{0} battles won", battlesWon));
        }

        return sb.ToString();
    }

    private IReadOnlyList<LeaderExportCandidateViewModel> BuildLeaders()
    {
        List<LeaderExportCandidateViewModel> result = [];

        IEnumerable<WorldStack> stacks = scenarioExporter
            .GetExportableStacks(WorldState, ViewPlayerId)
            .OrderByDescending(stack => stack.BattlesWon);

        foreach (WorldStack stack in stacks)
        {
            (WorldUnit leader, _, _) = stack.GetLeader(GameModel.Definitions);

            UnitDetailsViewModel details = UnitDetailsViewModel.CreateFromWorld(
                leader, WorldState.Players[stack.OwnerId], GameModel.Definitions, GameModel.Texts, GameModel.Stats);

            result.Add(new LeaderExportCandidateViewModel(
                stack.Id,
                new AssetSpriteKey.UnitPortrait(leader.DefinitionId),
                UnitNameResolver.Resolve(leader, GameModel.Definitions, GameModel.Texts),
                FormatLeaderSummary(leader.Level, stack.BattlesWon),
                details));
        }

        return result;
    }

    private void RefreshItems()
    {
        Items = BuildItems();
        SelectedItemsText = TrSpans("Selected items: {0:*} / {1:*}", selectedItems.Count, scenarioExporter.ItemLimit);
    }

    private IReadOnlyList<LeaderExportItemViewModel> BuildItems()
    {
        ImmutableArray<WorldItem> exportableItems = ScenarioExporter.GetExportableItems(WorldState, ViewPlayerId);
        List<(ItemDefinitionId DefinitionId, int? Charges)> keys = [];
        Dictionary<(ItemDefinitionId DefinitionId, int? Charges), List<WorldItem>> groups = [];

        foreach (WorldItem item in exportableItems)
        {
            (ItemDefinitionId DefinitionId, int? Charges) key = (item.DefinitionId, item.Charges);

            if (!groups.TryGetValue(key, out List<WorldItem>? group))
            {
                group = [];
                groups.Add(key, group);
                keys.Add(key);
            }

            group.Add(item);
        }

        bool limitReached = selectedItems.Count >= scenarioExporter.ItemLimit;
        List<LeaderExportItemViewModel> result = [];

        foreach ((ItemDefinitionId DefinitionId, int? Charges) key in keys.OrderByDescending(key => GetPrice(key.DefinitionId)))
        {
            List<WorldItem> items = groups[key];
            int selected = 0;

            foreach (WorldItem item in items)
            {
                if (selectedItems.Contains(item))
                {
                    selected++;
                }
            }

            ItemDetailsViewModel details = ItemDetailsViewModel.Create(
                key.DefinitionId, key.Charges, GameModel.Definitions, GameModel.Texts);

            result.Add(new LeaderExportItemViewModel(
                this,
                items,
                ItemIconResolver.Resolve(GameModel.Definitions, key.DefinitionId),
                ItemFormatter.FormatName(GameModel.Definitions, GameModel.Texts, key.DefinitionId, key.Charges),
                selected,
                canAdd: selected < items.Count && !limitReached,
                details));
        }

        return result;
    }

    private int GetPrice(ItemDefinitionId definitionId)
    {
        ItemDefinition definition = GameModel.Definitions.GetItem(definitionId);

        return definition.Value.Gold;
    }
}
