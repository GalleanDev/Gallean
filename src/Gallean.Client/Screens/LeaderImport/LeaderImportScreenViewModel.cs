using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Configuration;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class LeaderImportScreenViewModel : AbstractScreenViewModel
{
    private readonly ScenarioConfigurationScreenViewModel owner;
    private readonly IGameTextProvider texts;
    private readonly IGameDefinitionProvider definitions;
    private readonly WorldRace race;
    private readonly IUnitStatsProvider stats;

    public LeaderImportScreenViewModel(
        GameViewModel gameViewModel,
        ScenarioConfigurationScreenViewModel owner,
        IGameTextProvider texts,
        IGameDefinitionProvider definitions,
        WorldRace race,
        IReadOnlyList<LeaderEntry> entries)
    {
        this.owner = owner;
        this.texts = texts;
        this.definitions = definitions;
        this.race = race;

        stats = new UnitStatsProvider(definitions, new GlobalVariables(), ScenarioDefinition.Empty);
        Game = gameViewModel;
        Leaders = BuildLeaders(entries);
        SelectedLeader = ResolveSelection();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    public partial LeaderImportLeaderViewModel? SelectedLeader { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial IReadOnlyList<LeaderImportLeaderViewModel> Leaders { get; private set; } = [];

    public GameViewModel Game { get; }

    public bool IsEmpty => Leaders.Count == 0;

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    private static int CompareLeaders(LeaderImportLeaderViewModel left, LeaderImportLeaderViewModel right)
    {
        int rv;

        rv = right.IsAvailable.CompareTo(left.IsAvailable);
        if (rv != 0)
        {
            return rv;
        }

        rv = right.Entry.Leader.Unit.Level.CompareTo(left.Entry.Leader.Unit.Level);
        if (rv != 0)
        {
            return rv;
        }

        rv = right.Entry.Leader.BattlesWon.CompareTo(left.Entry.Leader.BattlesWon);
        if (rv != 0)
        {
            return rv;
        }

        rv = left.Entry.Id.Value.CompareTo(right.Entry.Id.Value);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }

    private IReadOnlyList<LeaderImportLeaderViewModel> BuildLeaders(IReadOnlyList<LeaderEntry> entries)
    {
        WorldRaceDefinition raceDefinition = definitions.GetRace(race);
        List<LeaderImportLeaderViewModel> result = [];

        foreach (LeaderEntry entry in entries)
        {
            ScenarioLeader leader = entry.Leader;
            List<AssetSpriteKey.ItemIcon> itemIcons = [];

            foreach (WorldItem item in leader.Equipment.Items.Values)
            {
                itemIcons.Add(ItemIconResolver.Resolve(definitions, item.DefinitionId));
            }

            UnitDetailsViewModel details = UnitDetailsViewModel.CreateFromWorld(leader.Unit, null, definitions, texts, stats);

            result.Add(new LeaderImportLeaderViewModel(
                entry,
                new AssetSpriteKey.UnitPortrait(leader.Unit.DefinitionId),
                UnitNameResolver.Resolve(leader.Unit, definitions, texts),
                LeaderFormatter.FormatLevel(leader.Unit.Level),
                LeaderFormatter.FormatBattlesWon(leader.BattlesWon),
                itemIcons,
                details,
                raceDefinition.Leaders.Contains(leader.Unit.DefinitionId)));
        }

        result.Sort(CompareLeaders);

        return result;
    }

    private LeaderImportLeaderViewModel? ResolveSelection()
    {
        foreach (LeaderImportLeaderViewModel leader in Leaders)
        {
            if (leader.IsAvailable)
            {
                return leader;
            }
        }

        return null;
    }

    [RelayCommand(CanExecute = nameof(CanImport))]
    private void Import()
    {
        if (SelectedLeader is null)
        {
            return;
        }

        owner.ImportLeader(SelectedLeader.Entry);
        CloseSelf();
    }

    private bool CanImport()
    {
        return SelectedLeader is not null && SelectedLeader.IsAvailable;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLeader))]
    private void Delete()
    {
        if (SelectedLeader is null)
        {
            return;
        }

        LeaderId id = SelectedLeader.Entry.Id;

        Game.OpenScreen(new ConfirmPromptViewModel(
            Tr("Delete leader"),
            TrSpans("Delete leader \"{0}\" permanently?", SelectedLeader.Name),
            Tr("Delete"),
            ConfirmPromptType.Danger,
            () => DeleteAsync(id)));
    }

    private bool HasSelectedLeader()
    {
        return SelectedLeader is not null;
    }

    private async Task DeleteAsync(LeaderId id)
    {
        if (await Game.TryDeleteLeaderAsync(id))
        {
            Leaders = BuildLeaders(await Game.EnumerateLeadersAsync());
            SelectedLeader = ResolveSelection();
        }
    }
}
