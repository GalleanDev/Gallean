using Gallean.Common;
using Gallean.Configuration;
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
            IsSkirmish: false,
            CanImportLeader: false,
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
            IsSkirmish: false,
            CanImportLeader: false,
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
            IsSkirmish: false,
            CanImportLeader: false,
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
        GameCampaign expansionCampaign = new(
            "expansion-campaign",
            "Expansion campaign",
            new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000")),
            IsSkirmish: false,
            [secondScenario]);
        GameCampaign skirmish = new(
            "skirmish",
            "Skirmish",
            new ScenarioRace(WorldRace.Neutral, new GlobalTextId("X000TG0004")),
            IsSkirmish: true,
            [firstScenario]);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns(Language.English);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        CampaignSelectScreenViewModel viewModel = new(gameViewModel, NullAssetPreloader.Instance, texts, definitions, [expansionCampaign, elvenCampaign, skirmish, campaign]);

        Assert.That(viewModel.Campaigns, Is.EqualTo([
            new CampaignSelectItemViewModel(campaign, new AssetSpriteKey.UnitPortrait(new UnitDefinitionId("G000UU0019")), "Empire", "Campaign", IsExpansion: false, SectionTitle: null),
            new CampaignSelectItemViewModel(elvenCampaign, new AssetSpriteKey.UnitPortrait(new UnitDefinitionId("G000UU8009")), "Elven Alliance", "Elven campaign", IsExpansion: false, SectionTitle: null),
            new CampaignSelectItemViewModel(expansionCampaign, new AssetSpriteKey.UnitPortrait(new UnitDefinitionId("G000UU0021")), "Empire", "Expansion campaign (Level 2+)", IsExpansion: true, "Expansions")
        ]));
        Assert.That(viewModel.SelectedCampaign, Is.EqualTo(new CampaignSelectItemViewModel(campaign, new AssetSpriteKey.UnitPortrait(new UnitDefinitionId("G000UU0019")), "Empire", "Campaign", IsExpansion: false, SectionTitle: null)));
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

        Assert.That(viewModel.SelectedCampaign, Is.EqualTo(new CampaignSelectItemViewModel(elvenCampaign, new AssetSpriteKey.UnitPortrait(new UnitDefinitionId("G000UU8009")), "Elven Alliance", "Elven campaign", IsExpansion: false, SectionTitle: null)));
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

        Assert.That(viewModel.Campaigns.Select(item => $"{item.Campaign.Id} {item.RaceName} | {item.Subtitle}"), Is.EqualTo([
            "C000CC0005 Undead Hordes | Dark Prophecy",
            "C000CC0002 Empire | Dark Prophecy",
            "C000CC0004 Legions of the Damned | Dark Prophecy",
            "C000CC0003 Mountain Clans | Dark Prophecy",
            "C000CC0006 Elven Alliance | Rise of the Elves",
            "C000CC0015 Undead Hordes | Servants of the Dark (Level 12+)",
            "C000CC0012 Empire | Guardians of the Light (Level 12+)",
            "C000CC0014 Legions of the Damned | Servants of the Dark (Level 12+)",
            "C000CC0013 Mountain Clans | Guardians of the Light (Level 12+)",
            "C000CC0200 Elven Alliance | The Alliance Crusade (Level 8+)"
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
