using Gallean.Common;
using Gallean.Engine;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class CampaignSelectScreenViewModelTests
{
    private sealed class TestAssetPreloader : IAssetPreloader
    {
        public List<AssetPreloadItem> Items { get; } = [];
        public bool IsReleased { get; private set; }

        public async Task<IDisposable> PreloadAsync(IEnumerable<AssetPreloadItem> items)
        {
            Items.AddRange(items);

            return new ActionDisposable(() => IsReleased = true);
        }
    }

    [Test]
    public async Task SelectionTest()
    {
        GameScenario firstScenario = new(
            "scenario-1",
            "First scenario",
            "A quiet barony falls to a death cult.",
            "Destroy the cult",
            1,
            48,
            4,
            "Strategy First",
            []);
        GameScenario secondScenario = new(
            "scenario-2",
            "Second scenario",
            null,
            "Hold the pass",
            2,
            72,
            8,
            null,
            []);
        GameScenario elvenScenario = new(
            "elven-scenario",
            "Elven scenario",
            null,
            null,
            1,
            96,
            13,
            "Erin Martel",
            []);
        GameCampaign campaign = new(
            "campaign",
            "Campaign",
            new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000")),
            IsSkirmish: false,
            [firstScenario, secondScenario]);
        GameCampaign elvenCampaign = new(
            "elven-campaign",
            "Elven campaign",
            new ScenarioRace(WorldRace.Elf, new GlobalTextId("X000TG0013")),
            IsSkirmish: false,
            [elvenScenario]);
        GameCampaign skirmish = new(
            "skirmish",
            "Skirmish",
            new ScenarioRace(WorldRace.Neutral, new GlobalTextId("X000TG0004")),
            IsSkirmish: true,
            [firstScenario]);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        CampaignSelectScreenViewModel viewModel = new(gameViewModel, NullAssetPreloader.Instance, texts, definitions, [elvenCampaign, skirmish, campaign]);

        Assert.That(viewModel.Campaigns, Is.EqualTo([
            new CampaignSelectItemViewModel(campaign, "Empire"),
            new CampaignSelectItemViewModel(elvenCampaign, "Elven Alliance")
        ]));
        Assert.That(viewModel.SelectedCampaign, Is.EqualTo(new CampaignSelectItemViewModel(campaign, "Empire")));
        Assert.That(viewModel.FirstScenario, Is.EqualTo(firstScenario));
        Assert.That(viewModel.MainMenuBackground, Is.EqualTo(new MainMenuBackgroundViewModel(new AssetSpriteKey.MainMenuBackgroundV2(), new AssetAnimationKey.MainMenuBackgroundOverlay(1))));
        Assert.That(viewModel.Background, Is.Null);

        await viewModel.RevealBackgroundCommand.ExecuteAsync(null);

        MenuBackgroundViewModel background = Assert.NotNull(viewModel.Background);
        Assert.That(background.SpriteKey, Is.EqualTo(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Human)));
        Assert.That(background.OverlayKeys, Is.EqualTo([
            new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Human, 0),
            new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Human, 1)
        ]));

        viewModel.SelectedCampaign = viewModel.Campaigns[1];

        Assert.That(viewModel.SelectedCampaign, Is.EqualTo(new CampaignSelectItemViewModel(elvenCampaign, "Elven Alliance")));
        Assert.That(viewModel.FirstScenario, Is.EqualTo(elvenScenario));
        MenuBackgroundViewModel elvenBackground = Assert.NotNull(viewModel.Background);
        Assert.That(elvenBackground.SpriteKey, Is.EqualTo(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Elf)));
    }

    [Test]
    public async Task CampaignsTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.ShowCampaignSelectCommand.ExecuteAsync(null);

        CampaignSelectScreenViewModel viewModel = Assert.IsOfType<CampaignSelectScreenViewModel>(gameViewModel.RootScreen);

        Assert.That(viewModel.Campaigns.Select(item => $"{item.Campaign.Id} {item.RaceName}"), Is.EqualTo([
            "C000CC0005 Undead Hordes",
            "C000CC0002 Empire",
            "C000CC0004 Legions of the Damned",
            "C000CC0003 Mountain Clans",
            "C000CC0006 Elven Alliance"
        ]));
    }

    [Test]
    public async Task PreloadBackgroundsTest()
    {
        TestAssetPreloader preloader = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssetPreloader(preloader).Build();

        await gameViewModel.ShowCampaignSelectCommand.ExecuteAsync(null);

        Assert.That(preloader.Items, Is.EqualTo<IReadOnlyList<AssetPreloadItem>>([
            new AssetPreloadItem.Sprite(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Undead)),
            new AssetPreloadItem.Animation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Undead, 0)),
            new AssetPreloadItem.Animation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Undead, 1)),
            new AssetPreloadItem.Sprite(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Human)),
            new AssetPreloadItem.Animation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Human, 0)),
            new AssetPreloadItem.Animation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Human, 1)),
            new AssetPreloadItem.Sprite(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Heretic)),
            new AssetPreloadItem.Animation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Heretic, 0)),
            new AssetPreloadItem.Animation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Heretic, 1)),
            new AssetPreloadItem.Sprite(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Dwarf)),
            new AssetPreloadItem.Animation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Dwarf, 0)),
            new AssetPreloadItem.Animation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Dwarf, 1)),
            new AssetPreloadItem.Sprite(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Elf)),
            new AssetPreloadItem.Animation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Elf, 0)),
            new AssetPreloadItem.Animation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Elf, 1))
        ]));
        Assert.That(preloader.IsReleased, Is.False);

        gameViewModel.Dispose();

        Assert.That(preloader.IsReleased, Is.True);
    }
}
