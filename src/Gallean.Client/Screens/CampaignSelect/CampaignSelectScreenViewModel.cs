using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Configuration;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class CampaignSelectScreenViewModel : AbstractScreenViewModel
{
    private readonly IAssetPreloader assetPreloader;
    private readonly IGameTextProvider texts;
    private readonly IGameDefinitionProvider definitions;
    private readonly MusicQueue musicQueue;
    private readonly CleanupList assetLeases = new();
    private readonly IReadOnlyList<GameCampaign> campaigns;
    private IReadOnlyList<GameCampaign> customCampaigns;
    private bool isBackgroundRevealed;

    public CampaignSelectScreenViewModel(
        GameViewModel gameViewModel,
        IAssetPreloader assetPreloader,
        IGameTextProvider texts,
        IGameDefinitionProvider definitions,
        IReadOnlyList<GameCampaign> campaigns,
        IReadOnlyList<GameCampaign> customCampaigns)
    {
        this.assetPreloader = assetPreloader;
        this.texts = texts;
        this.definitions = definitions;
        this.campaigns = campaigns;
        this.customCampaigns = customCampaigns;
        musicQueue = MusicQueue.Create([new AssetSoundKey.MenuMusic()], isLooping: true);

        Game = gameViewModel;
        Campaigns = BuildItems();
        SelectedCampaign = Campaigns.Count > 0 ? Campaigns[0] : null;
    }

    [ObservableProperty]
    public partial IReadOnlyList<CampaignSelectItemViewModel> Campaigns { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FirstScenario))]
    [NotifyPropertyChangedFor(nameof(IsCustomCampaignSelected))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    public partial CampaignSelectItemViewModel? SelectedCampaign { get; set; }

    [ObservableProperty]
    public partial MenuBackgroundViewModel? Background { get; private set; }

    public MainMenuBackgroundViewModel MainMenuBackground { get; } = MainMenuBackgroundViewModel.Create();
    public GameViewModel Game { get; }
    public bool IsCustomCampaignSelected => SelectedCampaign?.Campaign.CustomScenarioId is not null;

    public GameScenario? FirstScenario
    {
        get
        {
            if (SelectedCampaign is null)
            {
                return null;
            }
            else
            {
                return SelectedCampaign.Campaign.Scenarios[0];
            }
        }
    }

    public override ScreenMusic Music => ScreenMusic.Tracks(musicQueue);
    public override AssetSoundKey OpenSound => new AssetSoundKey.InterfaceOpen();

    private static int CompareCampaigns(GameCampaign left, GameCampaign right)
    {
        bool leftCustom = left.CustomScenarioId is not null;
        bool rightCustom = right.CustomScenarioId is not null;
        int leftOrder = GetCampaignOrder(left.PlayRace.Race);
        int rightOrder = GetCampaignOrder(right.PlayRace.Race);
        int rv;

        rv = leftCustom.CompareTo(rightCustom);
        if (rv != 0)
        {
            return rv;
        }

        rv = leftOrder.CompareTo(rightOrder);
        if (rv != 0)
        {
            return rv;
        }

        rv = string.Compare(left.Id, right.Id, StringComparison.OrdinalIgnoreCase);
        if (rv != 0)
        {
            return rv;
        }

        rv = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        if (rv != 0)
        {
            return rv;
        }

        rv = string.CompareOrdinal(left.CustomScenarioId?.Value, right.CustomScenarioId?.Value);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }

    private static UnitDefinitionId GetLeader(IGameDefinitionProvider definitions, WorldRaceDefinition race, UnitLeaderCategory category)
    {
        foreach (UnitDefinitionId leaderId in race.Leaders)
        {
            UnitDefinition leader = definitions.GetUnit(leaderId);

            if (leader.LeaderCategory == category)
            {
                return leaderId;
            }
        }

        throw new InvalidOperationException($"Race {race.Race} has no {category} leader.");
    }

    private static int GetCampaignOrder(WorldRace race)
    {
        return race switch
        {
            WorldRace.Undead => 0,
            WorldRace.Human => 1,
            WorldRace.Heretic => 2,
            WorldRace.Dwarf => 3,
            WorldRace.Elf => 4,
            WorldRace.Neutral => 5
        };
    }

    public override void Dispose()
    {
        assetLeases.Dispose();
    }

    protected override void OnOpened()
    {
        PreloadBackgroundsAsync().FileAndForget();
    }

    private static List<AssetPreloadItem> GetBackgroundPreloadItems(IEnumerable<WorldRace> races)
    {
        List<AssetPreloadItem> items = [];

        foreach (WorldRace race in races)
        {
            MenuBackgroundViewModel? viewModel = MenuBackgroundViewModel.TryCreateRaceSelection(race);

            if (viewModel is not null)
            {
                items.AddRange(viewModel.GetPreloadItems());
            }
        }

        return items;
    }

    private async Task PreloadBackgroundsAsync()
    {
        assetLeases.Add(await assetPreloader.PreloadAsync(GetBackgroundPreloadItems(Campaigns.Select(item => item.Campaign.PlayRace.Race).Distinct())));
    }

    [RelayCommand]
    private async Task RevealBackgroundAsync()
    {
        if (SelectedCampaign is not null)
        {
            assetLeases.Add(await assetPreloader.PreloadAsync(GetBackgroundPreloadItems([SelectedCampaign.Campaign.PlayRace.Race])));
        }

        isBackgroundRevealed = true;
        ShowSelectedBackground();
    }

    partial void OnSelectedCampaignChanged(CampaignSelectItemViewModel? value)
    {
        if (isBackgroundRevealed)
        {
            ShowSelectedBackground();
        }
    }

    private void ShowSelectedBackground()
    {
        if (SelectedCampaign is null)
        {
            Background = null;
        }
        else
        {
            MenuBackgroundViewModel? viewModel = MenuBackgroundViewModel.TryCreateRaceSelection(SelectedCampaign.Campaign.PlayRace.Race);

            if (Background?.SpriteKey != viewModel?.SpriteKey)
            {
                Background = viewModel;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        if (SelectedCampaign is null || FirstScenario is null)
        {
            return;
        }

        Game.OpenScreen(new ScenarioConfigurationScreenViewModel(Game, texts, definitions, FirstScenario, [SelectedCampaign.Campaign.PlayRace]));
    }

    private bool CanStart()
    {
        return FirstScenario is not null;
    }

    private IReadOnlyList<CampaignSelectItemViewModel> BuildItems()
    {
        List<GameCampaign> orderedCampaigns = [.. campaigns.Where(campaign => !campaign.IsSkirmish), .. customCampaigns];

        orderedCampaigns.Sort(CompareCampaigns);

        List<CampaignSelectItemViewModel> baseItems = [];
        List<CampaignSelectItemViewModel> expansionItems = [];
        List<CampaignSelectItemViewModel> customItems = [];
        HashSet<WorldRace> races = [];

        foreach (GameCampaign campaign in orderedCampaigns)
        {
            GlobalText raceName = texts.GetGlobalText(campaign.PlayRace.NameTextId);
            WorldRaceDefinition race = definitions.GetRace(campaign.PlayRace.Race);

            if (campaign.CustomScenarioId is not null)
            {
                AssetSpriteKey.UnitPortrait portraitKey = new(GetLeader(definitions, race, UnitLeaderCategory.Explorer));
                string? sectionTitle = customItems.Count == 0 ? Tr("Custom") : null;

                customItems.Add(new CampaignSelectItemViewModel(campaign, portraitKey, raceName.Text, campaign.Name, IsExpansion: true, sectionTitle));
            }
            else if (races.Add(campaign.PlayRace.Race))
            {
                AssetSpriteKey.UnitPortrait portraitKey = new(GetLeader(definitions, race, UnitLeaderCategory.Fighter));
                string name = CampaignFormatter.FormatName(campaign);

                baseItems.Add(new CampaignSelectItemViewModel(campaign, portraitKey, raceName.Text, name, IsExpansion: false, SectionTitle: null));
            }
            else
            {
                AssetSpriteKey.UnitPortrait portraitKey = new(GetLeader(definitions, race, UnitLeaderCategory.Mage));
                string name = CampaignFormatter.FormatName(campaign);
                string level = Tr("Level {0}+", campaign.Scenarios[0].SuggestedLevel);
                string subtitle = $"{name} ({level})";
                string? sectionTitle = expansionItems.Count == 0 ? Tr("Expansions") : null;

                expansionItems.Add(new CampaignSelectItemViewModel(campaign, portraitKey, raceName.Text, subtitle, IsExpansion: true, sectionTitle));
            }
        }

        return [.. baseItems, .. expansionItems, .. customItems];
    }

    [RelayCommand]
    private async Task ImportAsync(IReadOnlyList<ScenarioImportFile>? files)
    {
        if (files is not null)
        {
            foreach (ScenarioImportFile file in files)
            {
                GameCampaign? campaign = await Game.TryImportCustomCampaignAsync(file);

                if (campaign is not null)
                {
                    customCampaigns = [.. customCampaigns.Where(existing => existing.CustomScenarioId != campaign.CustomScenarioId), campaign];
                    Campaigns = BuildItems();
                    SelectedCampaign = Campaigns.First(item => item.Campaign == campaign);
                }
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        if (SelectedCampaign is not null && SelectedCampaign.Campaign.CustomScenarioId is not null)
        {
            CustomScenarioId id = SelectedCampaign.Campaign.CustomScenarioId.Value;

            Game.OpenScreen(new ConfirmPromptViewModel(
                TrSpans("Delete custom saga"),
                TrSpans("Delete \"{0}\" permanently?", SelectedCampaign.Campaign.Name),
                Tr("Delete"),
                ConfirmPromptType.Danger,
                () => DeleteAsync(id)));
        }
    }

    private bool CanDelete()
    {
        return IsCustomCampaignSelected;
    }

    private async Task DeleteAsync(CustomScenarioId id)
    {
        if (await Game.TryDeleteCustomScenarioAsync(id))
        {
            customCampaigns = [.. customCampaigns.Where(campaign => campaign.CustomScenarioId != id)];
            Campaigns = BuildItems();
            SelectedCampaign = Campaigns.Count > 0 ? Campaigns[0] : null;
        }
    }

    protected override bool TryClose()
    {
        Game.ShowMainMenuCommand.Execute(null);

        return true;
    }
}
