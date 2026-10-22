using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class CampaignSelectScreenViewModel : AbstractScreenViewModel
{
    private readonly IAssetPreloader assetPreloader;
    private readonly IGameTextProvider texts;
    private readonly IGameDefinitionProvider definitions;
    private readonly MusicQueue musicQueue;
    private readonly CleanupList assetLeases = new();
    private bool isBackgroundRevealed;

    public CampaignSelectScreenViewModel(
        GameViewModel gameViewModel,
        IAssetPreloader assetPreloader,
        IGameTextProvider texts,
        IGameDefinitionProvider definitions,
        IReadOnlyList<GameCampaign> campaigns)
    {
        this.assetPreloader = assetPreloader;
        this.texts = texts;
        this.definitions = definitions;
        musicQueue = MusicQueue.Create([new AssetSoundKey.MenuMusic()], isLooping: true);

        Game = gameViewModel;

        IOrderedEnumerable<GameCampaign> orderedCampaigns = campaigns
            .Where(campaign => !campaign.IsSkirmish)
            .OrderBy(campaign => GetCampaignOrder(campaign.PlayRace.Race))
            .ThenBy(campaign => campaign.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(campaign => campaign.Id, StringComparer.OrdinalIgnoreCase);

        List<CampaignSelectItemViewModel> items = [];

        foreach (GameCampaign campaign in orderedCampaigns)
        {
            GlobalText raceName = texts.GetGlobalText(campaign.PlayRace.NameTextId);

            items.Add(new CampaignSelectItemViewModel(campaign, raceName.Text));
        }

        Campaigns = items;
        SelectedCampaign = Campaigns.Count > 0 ? Campaigns[0] : null;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FirstScenario))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial CampaignSelectItemViewModel? SelectedCampaign { get; set; }

    [ObservableProperty]
    public partial MenuBackgroundViewModel? Background { get; private set; }

    public MainMenuBackgroundViewModel MainMenuBackground { get; } = MainMenuBackgroundViewModel.Create();
    public GameViewModel Game { get; }
    public IReadOnlyList<CampaignSelectItemViewModel> Campaigns { get; }

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

    private static List<AssetPreloadItem> GetBackgroundPreloadItems(IEnumerable<CampaignSelectItemViewModel> campaigns)
    {
        List<AssetPreloadItem> items = [];

        foreach (CampaignSelectItemViewModel item in campaigns)
        {
            MenuBackgroundViewModel? viewModel = MenuBackgroundViewModel.TryCreateRaceSelection(item.Campaign.PlayRace.Race);

            if (viewModel is not null)
            {
                items.AddRange(viewModel.GetPreloadItems());
            }
        }

        return items;
    }

    private async Task PreloadBackgroundsAsync()
    {
        assetLeases.Add(await assetPreloader.PreloadAsync(GetBackgroundPreloadItems(Campaigns)));
    }

    [RelayCommand]
    private async Task RevealBackgroundAsync()
    {
        if (SelectedCampaign is not null)
        {
            assetLeases.Add(await assetPreloader.PreloadAsync(GetBackgroundPreloadItems([SelectedCampaign])));
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

    protected override bool TryClose()
    {
        Game.ShowMainMenuCommand.Execute(null);

        return true;
    }
}
