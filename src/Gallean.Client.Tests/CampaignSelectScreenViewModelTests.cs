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

    private static IReadOnlyList<string> FormatCustomItems(IReadOnlyList<CampaignSelectItemViewModel> items)
    {
        List<string> rows = [];

        foreach (CampaignSelectItemViewModel item in items)
        {
            if (item.Campaign.CustomScenarioId is not null)
            {
                rows.Add($"{item.SectionTitle} | {item.RaceName} | {item.Subtitle} | {item.Campaign.CustomScenarioId}");
            }
        }

        return rows;
    }

    [Test]
    public async Task SelectionTest()
    {
        GameScenario firstScenario = new(
            CustomScenarioId: null,
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
            CustomScenarioId: null,
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
            CustomScenarioId: null,
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
            CustomScenarioId: null,
            "campaign",
            "Campaign",
            new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000")),
            IsSkirmish: false,
            [firstScenario, secondScenario]);
        GameCampaign elvenCampaign = new(
            CustomScenarioId: null,
            "elven-campaign",
            "Elven campaign",
            new ScenarioRace(WorldRace.Elf, new GlobalTextId("X000TG0013")),
            IsSkirmish: false,
            [elvenScenario]);
        GameCampaign expansionCampaign = new(
            CustomScenarioId: null,
            "expansion-campaign",
            "Expansion campaign",
            new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000")),
            IsSkirmish: false,
            [secondScenario]);
        GameCampaign skirmish = new(
            CustomScenarioId: null,
            "skirmish",
            "Skirmish",
            new ScenarioRace(WorldRace.Neutral, new GlobalTextId("X000TG0004")),
            IsSkirmish: true,
            [firstScenario]);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns(Language.English);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        CampaignSelectScreenViewModel viewModel = new(gameViewModel, NullAssetPreloader.Instance, texts, definitions, [expansionCampaign, elvenCampaign, skirmish, campaign], customCampaigns: []);

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

    [Test]
    public async Task CustomCampaignsTest()
    {
        using Sandbox sb = Sandbox.Create();

        CustomScenarioStore customScenarioStore = new(sb.GetPath("scenarios"));

        await customScenarioStore.SaveAsync(CustomScenarioFile.Create(
            CustomScenarioType.Campaign,
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestScenarios", "GreywaterChronicle.csg"))));
        await customScenarioStore.SaveAsync(CustomScenarioFile.Create(CustomScenarioType.Campaign, "NOTACAMPAIGNFILE"u8.ToArray()));
        await customScenarioStore.SaveAsync(CustomScenarioFile.Create(
            CustomScenarioType.Scenario,
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestScenarios", "GreywaterCrossing.sg"))));

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithCustomScenarioStore(customScenarioStore).Build();

        await gameViewModel.ShowCampaignSelectCommand.ExecuteAsync(null);

        CampaignSelectScreenViewModel viewModel = Assert.IsOfType<CampaignSelectScreenViewModel>(gameViewModel.RootScreen);

        Assert.That(FormatCustomItems(viewModel.Campaigns), Is.EqualTo([
            "Custom | Undead Hordes | The Greywater Chronicle | c0d20b970c9606c338078e40bc9403902b08394516ce4f4c76284f93c9df3ad2"
        ]));
        Assert.That(viewModel.IsCustomCampaignSelected, Is.False);
    }

    [Test]
    public async Task ImportTest()
    {
        using Sandbox sb = Sandbox.Create();

        CustomScenarioStore customScenarioStore = new(sb.GetPath("scenarios"));
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithCustomScenarioStore(customScenarioStore).Build();

        await gameViewModel.ShowCampaignSelectCommand.ExecuteAsync(null);

        CampaignSelectScreenViewModel viewModel = Assert.IsOfType<CampaignSelectScreenViewModel>(gameViewModel.RootScreen);

        await viewModel.ImportCommand.ExecuteAsync([
            new("Greywater Chronicle.csg", File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestScenarios", "GreywaterChronicle.csg")))
        ]);

        CampaignSelectItemViewModel selected = Assert.NotNull(viewModel.SelectedCampaign);
        GameScenario firstScenario = Assert.NotNull(viewModel.FirstScenario);

        Assert.That(FormatCustomItems(viewModel.Campaigns), Is.EqualTo([
            "Custom | Undead Hordes | The Greywater Chronicle | c0d20b970c9606c338078e40bc9403902b08394516ce4f4c76284f93c9df3ad2"
        ]));
        Assert.That(FormatCustomItems([selected]), Is.EqualTo([
            "Custom | Undead Hordes | The Greywater Chronicle | c0d20b970c9606c338078e40bc9403902b08394516ce4f4c76284f93c9df3ad2"
        ]));
        Assert.That(selected.PortraitKey, Is.EqualTo(new AssetSpriteKey.UnitPortrait(new UnitDefinitionId("G000UU0097"))));
        Assert.That(
            $"{firstScenario.CustomScenarioId} | {firstScenario.Id} | {firstScenario.Name}",
            Is.EqualTo("c0d20b970c9606c338078e40bc9403902b08394516ce4f4c76284f93c9df3ad2 | S900SC0000 | Greywater Crossing"));
        Assert.That(viewModel.IsCustomCampaignSelected, Is.True);
        Assert.That(gameViewModel.Screens, Is.EqualTo([viewModel]));
        Assert.That(sb.GetTree(), Is.EquivalentTo([
            "scenarios/",
            "scenarios/c0d20b970c9606c338078e40bc9403902b08394516ce4f4c76284f93c9df3ad2.csg"
        ]));
    }

    [Test]
    public async Task ImportInvalidTest()
    {
        using Sandbox sb = Sandbox.Create();

        CustomScenarioStore customScenarioStore = new(sb.GetPath("scenarios"));
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithCustomScenarioStore(customScenarioStore).Build();

        await gameViewModel.ShowCampaignSelectCommand.ExecuteAsync(null);

        CampaignSelectScreenViewModel viewModel = Assert.IsOfType<CampaignSelectScreenViewModel>(gameViewModel.RootScreen);

        await viewModel.ImportCommand.ExecuteAsync([
            new("Greywater Crossing.sg", File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestScenarios", "GreywaterCrossing.sg")))
        ]);

        ErrorScreenViewModel error = Assert.IsOfType<ErrorScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(error.Message, Is.EqualTo("Failed to import Greywater Crossing.sg.\nArchive does not start with 'MQDB'."));
        Assert.That(sb.GetTree(), Is.Empty);
    }

    [Test]
    public async Task DeleteTest()
    {
        using Sandbox sb = Sandbox.Create();

        CustomScenarioStore customScenarioStore = new(sb.GetPath("scenarios"));

        await customScenarioStore.SaveAsync(CustomScenarioFile.Create(
            CustomScenarioType.Campaign,
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestScenarios", "GreywaterChronicle.csg"))));

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithCustomScenarioStore(customScenarioStore).Build();

        await gameViewModel.ShowCampaignSelectCommand.ExecuteAsync(null);

        CampaignSelectScreenViewModel viewModel = Assert.IsOfType<CampaignSelectScreenViewModel>(gameViewModel.RootScreen);

        Assert.That(viewModel.DeleteCommand.CanExecute(null), Is.False);

        viewModel.SelectedCampaign = viewModel.Campaigns.First(item => item.Campaign.CustomScenarioId is not null);
        viewModel.DeleteCommand.Execute(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(FormatCustomItems(viewModel.Campaigns), Is.Empty);
        Assert.That(viewModel.SelectedCampaign, Is.SameAs(viewModel.Campaigns[0]));
        Assert.That(gameViewModel.Screens, Is.EqualTo([viewModel]));
        Assert.That(sb.GetTree(), Is.EqualTo(["scenarios/"]));
    }
}
