using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gallean.Common;
using Gallean.Configuration;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

[Explicit]
[NonParallelizable]
[Category("Visual")]
public sealed class VisualTests
{
    private const int BaseWidth = 1366;
    private const int BaseHeight = 768;

    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(60);

    private IAssetProvider assets = null!;
    private CleanupList cleanupList = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        assets = TestAssets.LoadOrIgnore();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        (assets as IDisposable)?.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        cleanupList = new CleanupList();
    }

    [TearDown]
    public void TearDown()
    {
        cleanupList.Dispose();
    }

    private (GameViewModel Game, GameView Window) Initialize(Sandbox sb, int width, int height)
    {
        NullAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder()
            .WithAssets(assets)
            .WithAudioPlayer(audioPlayer)
            .WithSaveStore(new GameSaveStore(sb.GetPath("saves"), 10))
            .WithSettingsStore(new GameSettingsStore(sb.GetPath("config")))
            .Build();

        GameView view = new()
        {
            DataContext = gameViewModel,
            WindowState = WindowState.Normal,
            SizeToContent = SizeToContent.Manual,
            Width = width,
            Height = height
        };

        AssetScope.SetAssets(view, assets);
        AudioScope.SetAudio(view, audioPlayer);
        RegisterWindow(view);
        cleanupList.Add(gameViewModel);
        view.Show();

        return (gameViewModel, view);
    }

    private void RegisterWindow(Window window)
    {
        cleanupList.Add(new ActionDisposable(() =>
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }));
    }

    private GameModel LoadScenario(WorldState? worldState)
    {
        GameLoader loader = new(new RandomProvider(0));
        (GameSession session, IGameTextProvider texts) = loader.LoadScenario("S107SC0000", worldState);

        return cleanupList.Add(new GameModel(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance));
    }

    private static BattleUnit MakeBattleUnit(int id, UnitDefinitionId definitionId, BattleSide side, GameModel gameModel)
    {
        UnitDefinition definition = gameModel.Definitions.GetUnit(definitionId);

        return new BattleUnit(
            new BattleUnitId(id),
            null,
            definitionId,
            side,
            definition.MaxHitpoints,
            Experience: 0,
            ExperienceGained: 0,
            definition.Level,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);
    }

    private static WorldPlayerBuildingDefinition MakeBuildingDefinition(WorldPlayerBuildingDefinitionId id)
    {
        return new WorldPlayerBuildingDefinition(
            Id: id,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);
    }

    private static ScenarioEventPopupMessage MakeScenarioPopupMessage(
        GameModel gameModel,
        string text,
        UnitDefinitionId unitId,
        bool isPortraitLeft)
    {
        UnitDefinition definition = gameModel.Definitions.GetUnit(unitId);
        GlobalText name = gameModel.Texts.GetGlobalText(definition.NameTextId);

        return new ScenarioEventPopupMessage(
            [
                new ScenarioTextSpan(text, ScenarioTextSpanType.Text)
            ],
            name.Text,
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(unitId)),
            isPortraitLeft);
    }

    private static async Task CaptureAsync(Window window)
    {
        string name = GetScreenshotName();

        await SaveScreenshotAsync(window, name);
    }

    private static async Task CaptureAsync(Window window, string step)
    {
        string name = GetScreenshotName();

        await SaveScreenshotAsync(window, $"{name}-{step}");
    }

    private static string GetScreenshotName()
    {
        string testName = Assert.NotNull(TestContext.CurrentContext.Test.MethodName);

        Assert.That(testName, Does.EndWith("Test"));

        string subject = testName.Remove(testName.Length - "Test".Length);
        StringBuilder sb = new();

        foreach (char letter in subject)
        {
            if (char.IsUpper(letter) && sb.Length > 0)
            {
                sb.Append('-');
            }

            sb.Append(char.ToLowerInvariant(letter));
        }

        return sb.ToString();
    }

    private static async Task SaveScreenshotAsync(Window window, string name)
    {
        await WaitUntilAsync(() => HasStableState(window));

        Assert.That(HasStableState(window), Is.True);

        await SkipFramesAsync(5);

        using WriteableBitmap bitmap = Assert.NotNull(window.CaptureRenderedFrame());

        string directory = Path.Combine(AppContext.BaseDirectory, "Screenshots");
        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, $"{name}.png");
        bitmap.Save(path, PngBitmapEncoderOptions.Default);

        TestContext.AddTestAttachment(path, name);
        await TestContext.Out.WriteLineAsync($"Wrote screenshot: {path}");
    }

    private static bool HasStableState(Window window)
    {
        foreach (ScreenPanel panel in window.GetVisualDescendants().OfType<ScreenPanel>())
        {
            foreach (Control screen in panel.Children)
            {
                if (screen.IsVisible && screen.Opacity < 1)
                {
                    return false;
                }
            }
        }

        foreach (WorldScreenView view in window.GetVisualDescendants().OfType<WorldScreenView>())
        {
            if (view.IsEffectivelyVisible)
            {
                WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(view.DataContext);
                WorldScreenSnapshot? snapshot = view.SnapshotWorker?.Current;

                if (!ReferenceEquals(snapshot?.RenderRequest, world.RenderRequest))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static async Task<WorldScreenView> WaitForWorldAsync(GameView window)
    {
        await SkipFramesAsync(5);

        GameViewModel game = Assert.IsOfType<GameViewModel>(window.DataContext);
        WorldScreenView view = window.GetVisualDescendants().OfType<WorldScreenView>().First();
        WorldScreenSnapshotWorker worker = Assert.NotNull(view.SnapshotWorker);

        await WaitUntilAsync(() =>
        {
            if (worker.Exception is not null)
            {
                return true;
            }

            return worker.Current is not null && !game.Screens.OfType<LoadingScreenViewModel>().Any();
        });

        Assert.That(worker.Exception, Is.Null);
        Assert.That(worker.Current, Is.Not.Null);
        Assert.That(game.Screens, Has.None.InstanceOf<LoadingScreenViewModel>());

        return view;
    }

    private static async Task WaitForBattleAsync(GameView window)
    {
        await SkipFramesAsync(5);

        GameViewModel game = Assert.IsOfType<GameViewModel>(window.DataContext);
        BattleScreenViewModel battle = Assert.IsOfType<BattleScreenViewModel>(game.RootScreen);
        BattleScreenView view = window.GetVisualDescendants().OfType<BattleScreenView>().First();
        Panel panel = view.GetVisualDescendants().OfType<Panel>().First(panel => panel.Classes.Contains("hud"));

        await WaitUntilAsync(() => battle.TransitionState is null && panel.Opacity >= 1);

        Assert.That(battle.TransitionState, Is.Null);
        Assert.That(panel.Opacity, Is.EqualTo(1.0));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (!condition() && stopwatch.Elapsed < WaitTimeout)
        {
            await SkipFramesAsync(5);
        }
    }

    private static void ClickMouseButton(GameView window, WorldScreenView view, WorldPosition position, MouseButton button)
    {
        Matrix viewMatrix = view.Camera.GetViewMatrix();
        Point tile = WorldTransform.ToWorldPoint(position);
        Point point = tile.Transform(viewMatrix);

        window.MouseDown(Assert.NotNull(view.World.TranslatePoint(point, window)), button);
    }

    private static async Task SkipFramesAsync(int count)
    {
        Dispatcher.UIThread.RunJobs();

        for (int frame = 0; frame < count; frame++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();

            await Task.Delay(TimeSpan.FromMilliseconds(16));
        }
    }

    [AvaloniaTest]
    public async Task MainMenuTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task MainMenuContinueTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        GameSaveEntry latestSave = new(
            new GameSaveId(1),
            Result<GameSaveMetadata>.FromValue(new GameSaveMetadata("Before the siege", GameSaveType.Manual, gameModel.ScenarioId, gameModel.ScenarioName, 12, new DateTimeOffset(2026, 3, 4, 18, 30, 0, TimeSpan.Zero), "0.1.0+2d40300")));

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, [latestSave]));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task MainMenuHoverTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        await SkipFramesAsync(5);

        MainMenuScreenView view = window.GetVisualDescendants().OfType<MainMenuScreenView>().First();
        Button button = view.NewCampaign;

        Point center = new(button.Bounds.Width / 2.0, button.Bounds.Height / 2.0);

        window.MouseMove(Assert.NotNull(button.TranslatePoint(center, window)));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task MainMenuContinueHoverTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        GameSaveEntry latestSave = new(
            new GameSaveId(1),
            Result<GameSaveMetadata>.FromValue(new GameSaveMetadata("Before the siege", GameSaveType.Manual, gameModel.ScenarioId, gameModel.ScenarioName, 12, new DateTimeOffset(2026, 3, 4, 18, 30, 0, TimeSpan.Zero), "0.1.0+2d40300")));

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, [latestSave]));

        await SkipFramesAsync(5);

        MainMenuScreenView view = window.GetVisualDescendants().OfType<MainMenuScreenView>().First();
        Button button = view.Continue;

        Point center = new(button.Bounds.Width / 2.0, button.Bounds.Height / 2.0);

        window.MouseMove(Assert.NotNull(button.TranslatePoint(center, window)));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task MainMenuFocusTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        await SkipFramesAsync(5);

        MainMenuScreenView view = window.GetVisualDescendants().OfType<MainMenuScreenView>().First();
        view.NewCampaign.Focus(NavigationMethod.Tab);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task MainMenuWideTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, 1600, 900);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task MainMenuUltraWideTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, 1720, 720);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task MainMenuFullHdTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, 1920, 1080);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task SettingsTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        SettingsScreenViewModel settings = new(gameViewModel);
        settings.SoundVolume = 35;
        settings.MusicVolume = 70;
        settings.RenderResolution = 75;
        settings.AutoSaveInterval = 10;

        gameViewModel.OpenScreen(settings);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task SettingsFocusTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));
        gameViewModel.OpenScreen(new SettingsScreenViewModel(gameViewModel));

        await SkipFramesAsync(5);

        CheckBox checkBox = window.GetVisualDescendants().OfType<CheckBox>().First();
        checkBox.Focus(NavigationMethod.Tab);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task SettingsDropDownTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));
        gameViewModel.OpenScreen(new SettingsScreenViewModel(gameViewModel));

        await SkipFramesAsync(5);

        ComboBox comboBox = window.GetVisualDescendants().OfType<ComboBox>().First();
        comboBox.IsDropDownOpen = true;

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task SettingsUiScaleTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, 1920, 1080);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        SettingsScreenViewModel settings = new(gameViewModel);
        settings.UiScale = new UiScaleOption(150);

        gameViewModel.OpenScreen(settings);

        await SkipFramesAsync(5);

        ComboBox comboBox = window.GetVisualDescendants().OfType<ComboBox>().First();
        comboBox.IsDropDownOpen = true;

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task LoadGameTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Before the siege", GameSaveType.Manual, gameModel.ScenarioId, gameModel.ScenarioName, 12, new DateTimeOffset(2026, 3, 4, 18, 30, 0, TimeSpan.Zero), "0.1.0+2d40300"),
            gameModel.Snapshot.WorldState);

        await store.SaveAsync(
            new GameSaveId(2),
            new GameSaveMetadata("First contact", GameSaveType.Manual, gameModel.ScenarioId, gameModel.ScenarioName, 3, new DateTimeOffset(2026, 3, 1, 9, 15, 0, TimeSpan.Zero), "0.1.0+2d40300"),
            gameModel.Snapshot.WorldState);
        sb.WriteTextFile(Path.Combine("saves", "save-3.json.gz"), "damaged");

        await using (FileStream stream = File.Create(sb.GetPath(Path.Combine("saves", "save-4.json.gz"))))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write("""{"formatVersion":34,"metadata":{"name":"Old campaign","type":"Manual","scenarioId":"S137SC0000","scenarioName":"Conquest of Thelena","turnNumber":21,"savedAt":"2026-02-20T21:45:00+00:00","savedBy":"0.0.9+1c2b3a4d5e6f"},"worldState":{}}"""u8);
        }

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task LoadGameScrollTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        GameSaveStore store = new(sb.GetPath("saves"), 10);

        for (int index = 1; index <= 8; index++)
        {
            await store.SaveAsync(
                new GameSaveId(index),
                new GameSaveMetadata($"Autosave {index}", GameSaveType.Turn, gameModel.ScenarioId, gameModel.ScenarioName, index, new DateTimeOffset(2026, 3, index, 18, 30, 0, TimeSpan.Zero), "0.1.0+2d40300"),
                gameModel.Snapshot.WorldState);
        }

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task LoadGameEmptyTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task SaveGameTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Before the siege", GameSaveType.Manual, gameModel.ScenarioId, gameModel.ScenarioName, 12, new DateTimeOffset(2026, 3, 4, 18, 30, 0, TimeSpan.Zero), "0.1.0+2d40300"),
            gameModel.Snapshot.WorldState);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        GameMenuViewModel menu = new(gameModel, gameViewModel);
        gameViewModel.OpenScreen(menu);

        await menu.SaveGameCommand.ExecuteAsync(null);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task SaveGameEmptyTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));

        GameMenuViewModel menu = new(gameModel, gameViewModel);
        gameViewModel.OpenScreen(menu);

        await menu.SaveGameCommand.ExecuteAsync(null);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CampaignSelectTest()
    {
        using Sandbox sb = Sandbox.Create();

        (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new CampaignSelectScreenViewModel(gameViewModel, NullAssetPreloader.Instance, texts, definitions, campaigns));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CampaignSelectWideTest()
    {
        using Sandbox sb = Sandbox.Create();

        (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, 1600, 900);

        gameViewModel.ShowScreen(new CampaignSelectScreenViewModel(gameViewModel, NullAssetPreloader.Instance, texts, definitions, campaigns));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CampaignSelectUltraWideTest()
    {
        using Sandbox sb = Sandbox.Create();

        (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, 1720, 720);

        gameViewModel.ShowScreen(new CampaignSelectScreenViewModel(gameViewModel, NullAssetPreloader.Instance, texts, definitions, campaigns));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CampaignSelectFullHdTest()
    {
        using Sandbox sb = Sandbox.Create();

        (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, 1920, 1080);

        gameViewModel.ShowScreen(new CampaignSelectScreenViewModel(gameViewModel, NullAssetPreloader.Instance, texts, definitions, campaigns));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CampaignSelectUndeadTest()
    {
        using Sandbox sb = Sandbox.Create();

        (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        CampaignSelectScreenViewModel screen = new(gameViewModel, NullAssetPreloader.Instance, texts, definitions, campaigns);
        screen.SelectedCampaign = screen.Campaigns.First(item => item.Campaign.PlayRace.Race == WorldRace.Undead);
        gameViewModel.ShowScreen(screen);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CampaignSelectElfTest()
    {
        using Sandbox sb = Sandbox.Create();

        (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        CampaignSelectScreenViewModel screen = new(gameViewModel, NullAssetPreloader.Instance, texts, definitions, campaigns);
        screen.SelectedCampaign = screen.Campaigns.First(item => item.Campaign.PlayRace.Race == WorldRace.Elf);
        gameViewModel.ShowScreen(screen);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task ScenarioSelectTest()
    {
        using Sandbox sb = Sandbox.Create();

        (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new ScenarioSelectScreenViewModel(gameViewModel, NullAssetPreloader.Instance, texts, definitions, campaigns));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task RaceSelectTest()
    {
        using Sandbox sb = Sandbox.Create();

        (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();
        GameScenario scenario = campaigns[0].Scenarios[1];

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new RaceSelectScreenViewModel(gameViewModel, NullAssetPreloader.Instance, texts, definitions, scenario));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task ScenarioConfigurationTest()
    {
        using Sandbox sb = Sandbox.Create();

        (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();
        GameCampaign campaign = campaigns[2];

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new ScenarioConfigurationScreenViewModel(gameViewModel, texts, definitions, campaign.Scenarios[0], [campaign.PlayRace]));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BriefingTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new BriefingScreenViewModel(
            "Antagonists",
            "Before long, Hubert de Lalye had assembled a considerable force. Moving north, his army met little resistance, and those cities which refused his ruling were quickly crushed. The heads of all dissenters were displayed on pikes throughout the Empire... warning all of the fate of those who dared oppose Hubert de Lalye's rule.\n\nBy that time, news of Uther's return had spread. Uther was reported to be leading an army of Clansmen down the peaks of Griffin Heights to cleanse the Empire of his father's enemies.\n\nThe emperor Demosthene decided to move his court north, to the fortified city of Fhindar. Inquisitor Phillip d'Agincourt resided there; he would surely ensure the emperor's safety in these grievous times. Demosthene hastily prepared his troops, knowing that the divided forces would shortly meet.",
            new ScenarioTextFormatter(
            [
                new ScenarioName("Hubert de Lalye", ScenarioNameType.Character),
                new ScenarioName("Uther", ScenarioNameType.Character),
                new ScenarioName("Demosthene", ScenarioNameType.Character),
                new ScenarioName("Fhindar", ScenarioNameType.Place),
                new ScenarioName("Empire", ScenarioNameType.Faction)
            ]),
            "Destroy Hubert de Lalye",
            MenuBackgroundViewModel.TryCreateRaceSelection(WorldRace.Human),
            new AssetSoundKey.BriefingVoice(new ScenarioId("S095SC0000")),
            new NullAudioPlayer(),
            async () => { }));

        await SkipFramesAsync(5);
        window.MouseDown(new Point(8, 8), MouseButton.Left);
        window.MouseUp(new Point(8, 8), MouseButton.Left);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task LoadingTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        MainMenuScreenViewModel menu = new(gameViewModel, saves: []);
        LoadingScreenViewModel loading = new("Loading data…");

        gameViewModel.ShowScreen(menu);
        await SkipFramesAsync(5);
        gameViewModel.OpenScreen(loading);
        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { menu, loading }));

        Control container = Assert.NotNull(window.ScreenHost.ContainerFromItem(loading));

        Assert.That(container.Opacity, Is.Zero);
        Assert.That(loading.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Default));

        await WaitUntilAsync(() => container.Opacity >= 1);

        Assert.That(container.Opacity, Is.EqualTo(1.0));
        Assert.That(loading.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Wait));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task GameEndedVictoryTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));
        gameViewModel.OpenScreen(new GameEndedScreenViewModel(
            isVictory: true,
            "Antagonists",
            "The defeat of Hubert de Lalye raised morale in the beleaguered realm. No one thought Uther capable of overcoming such a treacherous enemy.\n\nThe Emperor Demosthene's illness miraculously healed, his bitter heart comforted by the return of his long lost son, Uther. He knew he finally had the means to restore peace to his land...",
            new ScenarioTextFormatter(
            [
                new ScenarioName("Hubert de Lalye", ScenarioNameType.Character),
                new ScenarioName("Uther", ScenarioNameType.Character),
                new ScenarioName("Demosthene", ScenarioNameType.Character),
                new ScenarioName("Fhindar", ScenarioNameType.Place),
                new ScenarioName("Empire", ScenarioNameType.Faction)
            ]),
            MenuBackgroundViewModel.TryCreateRaceSelection(WorldRace.Human),
            ScreenMusic.None,
            gameViewModel,
            canContinueCampaign: false));

        await SkipFramesAsync(5);
        window.MouseDown(new Point(8, 8), MouseButton.Left);
        window.MouseUp(new Point(8, 8), MouseButton.Left);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task GameEndedDefeatTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));
        gameViewModel.OpenScreen(new GameEndedScreenViewModel(
            isVictory: false,
            "Antagonists",
            "You have been defeated, the objective was completed by the enemy.",
            new ScenarioTextFormatter([]),
            MenuBackgroundViewModel.TryCreateRaceSelection(WorldRace.Human),
            ScreenMusic.None,
            gameViewModel,
            canContinueCampaign: false));

        await SkipFramesAsync(5);
        window.MouseDown(new Point(8, 8), MouseButton.Left);
        window.MouseUp(new Point(8, 8), MouseButton.Left);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task GameEndedContinueCampaignTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameViewModel.StartGameCommand.ExecuteAsync(new GameConfiguration("S107SC0000", Configuration: null));

        BriefingScreenViewModel briefing = Assert.IsOfType<BriefingScreenViewModel>(gameViewModel.TopScreen);

        await briefing.ContinueCommand.ExecuteAsync(null);
        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(new GameEndedScreenViewModel(
            isVictory: true,
            "Antagonists",
            "The defeat of Hubert de Lalye raised morale in the beleaguered realm. No one thought Uther capable of overcoming such a treacherous enemy.",
            new ScenarioTextFormatter(
            [
                new ScenarioName("Hubert de Lalye", ScenarioNameType.Character),
                new ScenarioName("Uther", ScenarioNameType.Character)
            ]),
            MenuBackgroundViewModel.TryCreateRaceSelection(WorldRace.Human),
            ScreenMusic.None,
            gameViewModel,
            canContinueCampaign: true));

        await SkipFramesAsync(5);
        window.MouseDown(new Point(8, 8), MouseButton.Left);
        window.MouseUp(new Point(8, 8), MouseButton.Left);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task FatalErrorWindowTest()
    {
        FatalErrorWindow window = new("Failed to start the game", "Access to the path '/home/player/.local/share/Gallean/Saves' is denied.", "/home/player/.local/share/Gallean/Logs/client-20260101-120000-000Z.log");
        RegisterWindow(window);
        window.Show();

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task FatalErrorModalTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new MainMenuScreenViewModel(gameViewModel, saves: []));
        gameViewModel.OpenScreen(new ErrorScreenViewModel(
            "An unexpected error occurred.\nAttempted to divide by zero.",
            "Exit",
            () => { },
            "/home/player/.local/share/Gallean/Logs/client-20260101-120000-000Z.log"));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task AssetPackNotFoundWindowTest()
    {
        AssetPackErrorWindow window = new();
        window.DataContext = new AssetPackErrorViewModel(new InstalledAssetPackNotFoundException(), "/home/player/Gallean", new ClassicDesktopStyleApplicationLifetime());
        RegisterWindow(window);
        window.Show();

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task AssetPackIncompatibleWindowTest()
    {
        AssetPackUnsupportedFormatException innerException = new("Unsupported asset pack format '22'.");
        AssetPackErrorWindow window = new();
        window.DataContext = new AssetPackErrorViewModel(new InstalledAssetPackIncompatibleException("/home/player/Gallean/assets.galleanpack", innerException), "/home/player/Gallean", new ClassicDesktopStyleApplicationLifetime());
        RegisterWindow(window);
        window.Show();

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task AssetPackNotEnhancedWindowTest()
    {
        AssetPackErrorWindow window = new()
        {
            DataContext = new AssetPackErrorViewModel(
                new InstalledAssetPackNotEnhancedException(),
                "/home/player/Gallean",
                new ClassicDesktopStyleApplicationLifetime())
        };
        RegisterWindow(window);
        window.Show();

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldMoveReachTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        await WaitForWorldAsync(window);

        await world.HandleMapClickAsync(new WorldPosition(10, 14), null, 1);
        world.UpdateHover(new WorldPosition(8, 4), null);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldObjectiveCollapsedTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        world.ToggleObjectiveCommand.Execute(null);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldGameMenuTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<GameMenuViewModel>());

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldTutorialTest()
    {
        using Sandbox sb = Sandbox.Create();

        (GameSession session, IGameTextProvider texts) = new GameLoader(new RandomProvider(0)).LoadScenario(GameLoader.TutorialScenarioId, null);
        GameModel gameModel = new(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldScenarioDialogTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        UnitDefinitionId human = gameModel.Definitions.GetRace(WorldRace.Human).Units[0];
        UnitDefinitionId undead = gameModel.Definitions.GetRace(WorldRace.Undead).Units[0];

        ScenarioEventPopupViewModel popup = new(
        [
            MakeScenarioPopupMessage(gameModel, "The dead stir in the barrows to the north, my lord. Our scouts do not return.", human, isPortraitLeft: true),
            MakeScenarioPopupMessage(gameModel, "Your scouts serve a new master now. Nevendaar will kneel before the risen.", undead, isPortraitLeft: false),
            MakeScenarioPopupMessage(gameModel, "Then we will give them a proper burial. Rally the garrison!", human, isPortraitLeft: true)
        ]);

        gameViewModel.OpenScreen(popup);
        popup.CloseCommand.Execute(null);
        popup.CloseCommand.Execute(null);

        await SkipFramesAsync(5);
        window.MouseDown(new Point(8, 8), MouseButton.Left);
        window.MouseUp(new Point(8, 8), MouseButton.Left);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldScenarioLordDialogTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        UnitDefinitionId undead = gameModel.Definitions.GetRace(WorldRace.Undead).Units[0];
        AssetSpriteKey.LordFace lordFace = new(WorldRace.Elf, 0, IsLarge: true);

        ScenarioEventPopupViewModel popup = new(
        [
            new ScenarioEventPopupMessage(
                [
                    new ScenarioTextSpan("Hearth's Grove", ScenarioTextSpanType.Place),
                    new ScenarioTextSpan(" is silent tonight. Too silent for the season.", ScenarioTextSpanType.Text)
                ],
                "Lord",
                lordFace,
                IsPortraitLeft: true),
            MakeScenarioPopupMessage(gameModel, "The trees have already chosen their new master, elf.", undead, isPortraitLeft: false),
            new ScenarioEventPopupMessage(
                [
                    new ScenarioTextSpan("Then the trees will burn with you, as ", ScenarioTextSpanType.Text),
                    new ScenarioTextSpan("Lachla'an", ScenarioTextSpanType.Character),
                    new ScenarioTextSpan(" foretold.", ScenarioTextSpanType.Text)
                ],
                "Lord",
                lordFace,
                IsPortraitLeft: true)
        ]);

        gameViewModel.OpenScreen(popup);
        popup.CloseCommand.Execute(null);
        popup.CloseCommand.Execute(null);

        await SkipFramesAsync(5);
        window.MouseDown(new Point(8, 8), MouseButton.Left);
        window.MouseUp(new Point(8, 8), MouseButton.Left);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldScenarioNarrationTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        ScenarioEventPopupViewModel popup = new(
        [
            new ScenarioEventPopupMessage([new ScenarioTextSpan("THE SIEGE OF TEMPERANCE", ScenarioTextSpanType.Text)], "", null, true)
        ]);

        gameViewModel.OpenScreen(popup);

        await SkipFramesAsync(5);
        window.MouseDown(new Point(8, 8), MouseButton.Left);
        window.MouseUp(new Point(8, 8), MouseButton.Left);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldRuinLootTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(LootScreenViewModel.CreateForRuin(
            gameModel,
            new WorldResourceSet(250, 0, 0, 0, 0, 0),
            new WorldItem(new WorldItemId("Item0001"), new ItemDefinitionId("G000IG0001"), Charges: null)));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldBagLootTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(LootScreenViewModel.CreateForItems(
            gameModel,
            new WorldInventory(
            [
                new WorldItem(new WorldItemId("HealingPotion"), new ItemDefinitionId("G000IG0001"), Charges: null),
                new WorldItem(new WorldItemId("ManaPotion"), new ItemDefinitionId("G000IG0002"), Charges: null),
                new WorldItem(new WorldItemId("SecondHealingPotion"), new ItemDefinitionId("G000IG0001"), Charges: null)
            ])));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldSpellLootTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(LootScreenViewModel.CreateForSpells(
            gameModel,
            [new WorldSpellDefinitionId("G000SS0008"), new WorldSpellDefinitionId("G000SS0036")]));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldThiefActionResultTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(new ThiefActionResultScreenViewModel(WorldThiefActionType.StealGold, isSuccess: true));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldHotseatTurnTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players = state.Players;

        foreach (WorldPlayer player in state.Players.Values)
        {
            if (player.Race != WorldRace.Neutral)
            {
                players = players.SetItem(player.Id, player with { IsAi = false });
            }
        }

        GameModel gameModel = LoadScenario(state with { Players = players });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldZoomedOutTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        WorldScreenView view = await WaitForWorldAsync(window);
        view.Camera.SetZoom(0.75);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldFogDisabledTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.DevTools.IsFogEnabled = false;
        gameViewModel.ShowScreen(viewModel);

        await WaitForWorldAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldDevToolsWarningTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForWorldAsync(window);

        gameViewModel.DevTools.ToggleCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<ConfirmPromptViewModel>());

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldAttackPromptTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition undead = scenarioModel.Definitions.GetRace(WorldRace.Undead);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition enemyDefinition = scenarioModel.Definitions.GetUnit(undead.Leaders[0]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit enemyLeader = new(new WorldUnitId("enemyLeader"), enemyDefinition.Id, enemyDefinition.MaxHitpoints, 0, enemyDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(1, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            enemy.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(enemyLeader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithDefinition(leaderDefinition)
            .WithDefinition(enemyDefinition)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        await WaitForWorldAsync(window);

        await world.HandleMapClickAsync(stack.Position, null, 1);
        await world.HandleMapClickAsync(new WorldPosition(3, 3), null, 1);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<AttackPromptViewModel>());

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldUnknownAttackPromptTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition undead = scenarioModel.Definitions.GetRace(WorldRace.Undead);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition enemyDefinition = scenarioModel.Definitions.GetUnit(undead.Leaders[0]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit enemyLeader = new(new WorldUnitId("enemyLeader"), enemyDefinition.Id, enemyDefinition.MaxHitpoints, 0, enemyDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(1, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
                new WorldStackId("enemy"),
                enemy.Id,
                new WorldPosition(4, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(enemyLeader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Concealed = true
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithDefinition(leaderDefinition)
            .WithDefinition(enemyDefinition)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        await WaitForWorldAsync(window);

        await world.HandleMapClickAsync(stack.Position, null, 1);
        await world.HandleMapClickAsync(new WorldPosition(3, 3), null, 1);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<AttackPromptViewModel>());

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldDevToolsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForWorldAsync(window);

        gameViewModel.DevTools.ToggleCommand.Execute(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await prompt.ConfirmCommand.ExecuteAsync(null);

        gameViewModel.DevTools.IsHitboxOverlayEnabled = true;
        gameViewModel.DevTools.IsFogEnabled = false;

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldObjectiveMarkerTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.DevTools.IsFogEnabled = false;
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);
        view.Camera.LookAt(WorldTransform.ToWorldPoint(45, 26));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldBlendBordersTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.BuildMap([
            "................",
            "................",
            "..HHHH....~~~...",
            "..HHHH....~~~...",
            "..HHHH....~~~...",
            "..HHHH..........",
            "................",
            "......H.........",
            "................",
            "..UUU......H....",
            "..UUU.....H.....",
            "..UUU...........",
            "................",
            "................"
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        cleanupList.Add(gameModel);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.DevTools.IsFogEnabled = false;
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);
        view.Camera.LookAt(WorldTransform.ToWorldPoint(8, 7));
        view.Camera.SetZoom(1.25);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldMixedCoastBordersTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.BuildMap([
            "................",
            "................",
            ".......~~~~.....",
            "......~~RRR~~...",
            ".....~~RRRRR~~..",
            "......~~RRR~~...",
            ".......~~~~.....",
            "................",
            "................",
            "......~~~~~~....",
            "................",
            "................",
            "................",
            "................"
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        cleanupList.Add(gameModel);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.DevTools.IsFogEnabled = false;
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);
        view.Camera.LookAt(WorldTransform.ToWorldPoint(8, 5));
        view.Camera.SetZoom(1.25);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldCityShieldsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.DevTools.IsFogEnabled = false;
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);
        view.Camera.SetZoom(1.25);
        view.Camera.LookAt(WorldTransform.ToWorldPoint(new WorldPosition(10, 14)));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldRoadsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.DevTools.IsFogEnabled = false;
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);
        view.Camera.LookAt(WorldTransform.ToWorldPoint(new WorldPosition(12, 11)));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldCoastTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.DevTools.IsFogEnabled = false;
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);
        view.Camera.LookAt(WorldTransform.ToWorldPoint(new WorldPosition(6, 0)));
        view.Camera.SetZoom(1.25);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldSelectedPathTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForWorldAsync(window);

        await viewModel.HandleMapClickAsync(new WorldPosition(10, 14), null, 1);
        viewModel.UpdateHover(new WorldPosition(13, 18), null);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldActionWheelTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        WorldPlan plan = WorldPlan.For(state);
        WorldStack hero = Assert.NotNull(plan.GetStackAt(new WorldPosition(10, 14)));
        WorldItem scroll = new(new WorldItemId("Scroll"), new ItemDefinitionId("G000IG5001"), Charges: null);
        WorldStack upgraded = hero with
        {
            PendingLeaderUpgrades = 1,
            Equipment = hero.Equipment.With(LeaderEquipmentSlot.Battle1, scroll)
        };
        WorldStack escort = hero with { Id = new WorldStackId("Escort"), Position = new WorldPosition(11, 14) };

        GameModel gameModel = LoadScenario(state with { Stacks = state.Stacks.SetItem(hero.Id, upgraded).Add(escort.Id, escort) });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);

        await viewModel.HandleMapClickAsync(new WorldPosition(10, 14), null, 1);
        view.Camera.LookAt(WorldTransform.ToWorldPoint(new WorldPosition(4, 6)));

        await CaptureAsync(window);

        Button socket = window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("wheel")).Skip(2).First();
        window.MouseMove(Assert.NotNull(socket.TranslatePoint(new Point(23, 23), window)));

        await CaptureAsync(window, "hover");
    }

    [AvaloniaTest]
    public async Task WorldStackCardClickTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);
        view.Camera.LookAt(WorldTransform.ToWorldPoint(new WorldPosition(4, 6)));

        Button card = window.GetVisualDescendants().OfType<Button>().First(button => button.Classes.Contains("world-stack-card"));
        WorldStackItemViewModel item = Assert.IsOfType<WorldStackItemViewModel>(card.DataContext);
        Point center = Assert.NotNull(card.TranslatePoint(new Point(65, 40), window));

        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(viewModel));
        Assert.That(item.IsSelected, Is.True);
        Assert.That(view.Camera.Target, Is.EqualTo(WorldTransform.ToWorldPoint(item.Position)));

        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(viewModel));
        Assert.That(item.IsSelected, Is.True);
    }

    [AvaloniaTest]
    public async Task WorldStackCardButtonsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);

        await viewModel.HandleMapClickAsync(new WorldPosition(10, 14), null, 1);
        view.Camera.LookAt(WorldTransform.ToWorldPoint(new WorldPosition(4, 6)));

        await SkipFramesAsync(1);

        Button card = window.GetVisualDescendants().OfType<Button>().First(button => button.Classes.Contains("selected"));
        Panel item = Assert.IsOfType<Panel>(card.Parent);
        Button inspect = item.GetVisualDescendants().OfType<Button>().First(button => button.Classes.Contains("card"));
        window.MouseMove(Assert.NotNull(inspect.TranslatePoint(new Point(19, 19), window)));

        Panel inspectSlot = Assert.IsOfType<Panel>(inspect.Parent);
        OutlinedTextBlock caption = inspectSlot.GetVisualDescendants().OfType<OutlinedTextBlock>().First();

        Assert.That(inspect.IsPointerOver, Is.True);
        Assert.That(caption.IsEffectivelyVisible, Is.True);
        Assert.That(caption.Text, Is.EqualTo("Inspect"));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldStackBarCrowdedTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        WorldPlan plan = WorldPlan.For(state);
        WorldStack hero = Assert.NotNull(plan.GetStackAt(new WorldPosition(10, 14)));
        ImmutableSortedDictionary<WorldStackId, WorldStack> stacks = state.Stacks;

        for (int index = 0; index < 5; index++)
        {
            WorldStack escort = hero with { Id = new WorldStackId($"Escort{index}"), Position = new WorldPosition(11 + index, 14) };
            stacks = stacks.Add(escort.Id, escort);
        }

        GameModel gameModel = LoadScenario(state with { Stacks = stacks });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);

        await viewModel.HandleMapClickAsync(new WorldPosition(10, 14), null, 1);
        view.Camera.LookAt(WorldTransform.ToWorldPoint(new WorldPosition(4, 6)));

        await SkipFramesAsync(1);

        Button card = window.GetVisualDescendants().OfType<Button>().First(button => button.Classes.Contains("selected"));
        Panel item = Assert.IsOfType<Panel>(card.Parent);
        Button done = item.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("card")).Skip(1).First();
        window.MouseMove(Assert.NotNull(done.TranslatePoint(new Point(19, 19), window)));

        Panel doneSlot = Assert.IsOfType<Panel>(done.Parent);
        OutlinedTextBlock caption = doneSlot.GetVisualDescendants().OfType<OutlinedTextBlock>().First();

        Assert.That(done.IsPointerOver, Is.True);
        Assert.That(caption.IsEffectivelyVisible, Is.True);
        Assert.That(caption.Text, Is.EqualTo("Done"));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldStackInfoTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldPlan plan = WorldPlan.For(gameModel.World.State);
        WorldStack stack = Assert.NotNull(plan.GetStackAt(new WorldPosition(10, 14)));
        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(stack.Id, new WorldPosition(11, 14)));

        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);

        ClickMouseButton(window, view, new WorldPosition(11, 14), MouseButton.Right);
        Assert.IsOfType<WorldStackInfoViewModel>(viewModel.PositionInfo);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldTileInfoTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);

        ClickMouseButton(window, view, new WorldPosition(2, 9), MouseButton.Right);
        Assert.IsOfType<WorldTileInfoViewModel>(viewModel.PositionInfo);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldSiteInfoTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        WorldMerchantSite merchant = state.Sites.Values.OfType<WorldMerchantSite>().First();
        WorldMerchantSite movedMerchant = merchant with { Footprint = merchant.Footprint with { TopLeft = new WorldPosition(13, 11) } };

        GameModel gameModel = LoadScenario(state with { Sites = state.Sites.SetItem(merchant.Id, movedMerchant) });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);

        ClickMouseButton(window, view, new WorldPosition(14, 12), MouseButton.Right);
        Assert.IsOfType<WorldTileInfoViewModel>(viewModel.PositionInfo);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldSpiedRuinInfoTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        WorldPlan plan = WorldPlan.For(state);
        WorldStack hero = Assert.NotNull(plan.GetStackAt(new WorldPosition(10, 14)));
        WorldPlayer player = state.Players[hero.OwnerId];
        WorldRuin ruin = state.Fortifications.Values.OfType<WorldRuin>().First(candidate => candidate.Item is not null && !candidate.Garrison.IsEmpty);
        WorldRuin movedRuin = ruin with { Footprint = ruin.Footprint with { TopLeft = new WorldPosition(13, 11) } };

        GameModel gameModel = LoadScenario(state with
        {
            Fortifications = state.Fortifications.SetItem(ruin.Id, movedRuin),
            Players = state.Players.SetItem(player.Id, player with { SpiedRuins = player.SpiedRuins.Add(ruin.Id) })
        });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);

        ClickMouseButton(window, view, new WorldPosition(14, 12), MouseButton.Right);
        Assert.IsOfType<WorldTileInfoViewModel>(viewModel.PositionInfo);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldTombInfoTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        WorldPlan plan = WorldPlan.For(state);
        WorldStack hero = Assert.NotNull(plan.GetStackAt(new WorldPosition(10, 14)));
        (WorldUnit heroLeader, _, _) = hero.GetLeader(scenario.Definitions);
        WorldStack neutralStack = state.Stacks.Values.First(stack => state.Players[stack.OwnerId].Race == WorldRace.Neutral);
        (WorldUnit neutralLeader, _, _) = neutralStack.GetLeader(scenario.Definitions);

        WorldTomb heroTomb = new(new WorldTombId("Tomb0001"), new WorldPosition(12, 14), hero.OwnerId, heroLeader.DefinitionId, "Aldric", neutralStack.OwnerId, neutralLeader.DefinitionId, neutralLeader.Name, Turn: 1);
        WorldTomb neutralTomb = new(new WorldTombId("Tomb0002"), new WorldPosition(12, 14), neutralStack.OwnerId, neutralLeader.DefinitionId, LeaderName: null, hero.OwnerId, heroLeader.DefinitionId, heroLeader.Name, Turn: 1);

        GameModel gameModel = LoadScenario(state with { Tombs = state.Tombs.Add(heroTomb.Id, heroTomb).Add(neutralTomb.Id, neutralTomb) });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);

        ClickMouseButton(window, view, new WorldPosition(12, 14), MouseButton.Right);
        WorldTileInfoViewModel info = Assert.IsOfType<WorldTileInfoViewModel>(viewModel.PositionInfo);
        Assert.That(info.Title, Is.EqualTo("Here lies:"));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task WorldCityInfoTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        WorldScreenViewModel viewModel = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        WorldScreenView view = await WaitForWorldAsync(window);

        ClickMouseButton(window, view, new WorldPosition(8, 12), MouseButton.Right);
        Assert.IsOfType<WorldFortificationInfoViewModel>(viewModel.PositionInfo);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CityTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, new WorldFortificationId("S107FT0000")));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CityLockTypeTargetTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, new WorldFortificationId("S107FT0000"));
        gameViewModel.OpenScreen(management);

        CityTabViewModel tab = Assert.IsOfType<CityTabViewModel>(management.SelectedTab);
        tab.LockTypeCommand.Execute(null);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CityFullHdTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, 1920, 1080);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, new WorldFortificationId("S107FT0000")));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CityDeadUnitTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);
        UnitDefinition archerDefinition = scenarioModel.Definitions.GetUnit(humans.Units[1]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit deadSoldier = new(new WorldUnitId("deadSoldier"), soldierDefinition.Id, 0, 0, soldierDefinition.Level, [], null, false);
        WorldUnit deadArcher = new(new WorldUnitId("deadArcher"), archerDefinition.Id, 0, 0, archerDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(deadArcher, GridPosition.FrontBottom, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("visitors"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (deadSoldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(archerDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, city.Id);
        gameViewModel.OpenScreen(management);

        await CaptureAsync(window);

        CityTabViewModel tab = Assert.IsOfType<CityTabViewModel>(management.SelectedTab);
        UnitGridSlotViewModel source = tab.StackGrid.Slots.First(slot => slot.Position == GridPosition.FrontCenter);
        tab.DragDrop.TryBeginDrag(source.Reference);

        await CaptureAsync(window, "drag");
    }

    [AvaloniaTest]
    public async Task CityDragItemTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        ItemDefinition potionDefinition = scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0005"));

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder().Build());

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("visitors"),
                player.Id,
                city.Entrance,
                TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Potion"), potionDefinition.Id, Charges: null)])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potionDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, city.Id);
        gameViewModel.OpenScreen(management);

        CityTabViewModel tab = Assert.IsOfType<CityTabViewModel>(management.SelectedTab);
        tab.DragDrop.TryBeginDrag(tab.InventoryTransfer.Inventory.Items[0].Reference);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CityUpgradeTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, soldierDefinition.Level, [], null, false);
        WorldUnit garrisonSoldier = new(new WorldUnitId("garrisonSoldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldCityDefinition level1 = scenarioModel.Definitions.GetCity(new WorldCityDefinitionId(WorldRace.Human, 1));
        WorldCityDefinition level2 = scenarioModel.Definitions.GetCity(new WorldCityDefinitionId(WorldRace.Human, 2));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonSoldier, GridPosition.FrontBottom, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("visitors"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, city.Id);
        gameViewModel.OpenScreen(management);

        await CaptureAsync(window);

        CityTabViewModel tab = Assert.IsOfType<CityTabViewModel>(management.SelectedTab);
        tab.UpgradeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<ConfirmPromptViewModel>());

        await CaptureAsync(window, "prompt");
    }

    [AvaloniaTest]
    public async Task CityWithoutStackTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit garrisonSoldier = new(new WorldUnitId("garrisonSoldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, soldierDefinition.Level, [], null, false);

        WorldCityDefinition cityDefinition = scenarioModel.Definitions.GetCity(new WorldCityDefinitionId(WorldRace.Human, 1));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonSoldier, GridPosition.FrontBottom, false)
                .Build());

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, city.Id));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CityInventoryOverflowTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);

        ItemDefinition[] itemDefinitions =
        [
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0002")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0004")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0005")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG1001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG2001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG3001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG4001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG7101")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG6001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG7001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG9001"))
        ];

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit garrisonSoldier = new(new WorldUnitId("garrisonSoldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, soldierDefinition.Level, [], null, false);

        List<WorldItem> partyItems = [];
        List<WorldItem> cityItems = [];

        for (int index = 0; index < 40; index++)
        {
            partyItems.Add(new WorldItem(new WorldItemId($"PartyItem{index:D2}"), itemDefinitions[index % itemDefinitions.Length].Id, Charges: null));
            cityItems.Add(new WorldItem(new WorldItemId($"CityItem{index:D2}"), itemDefinitions[(index + 6) % itemDefinitions.Length].Id, Charges: null));
        }

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldState scenarioState = scenarioModel.Snapshot.WorldState;
        WorldFortification scenarioCity = scenarioState.Fortifications[new WorldFortificationId("S107FT0000")];

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(garrisonSoldier, GridPosition.FrontBottom, false)
                    .Build()) with
            {
                NameTextId = scenarioCity.NameTextId,
                Inventory = new WorldInventory([.. cityItems])
            };

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("visitors"),
                player.Id,
                city.Entrance,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([.. partyItems])
            };

        TestWorldEngineBuilder builder = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace);

        foreach (ItemDefinition itemDefinition in itemDefinitions)
        {
            builder.WithDefinition(itemDefinition);

            if (itemDefinition.EquipmentModifierId is { } modifierId)
            {
                builder.WithDefinition(scenarioModel.Definitions.GetUnitModifier(modifierId));
            }
        }

        (GameSession session, _) = builder.BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, city.Id));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CityLeaderDetailsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder().Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("visitors"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, city.Id));

        await SkipFramesAsync(5);

        PointerTiltDecorator portrait = window.GetVisualDescendants().OfType<PointerTiltDecorator>().First(candidate => candidate.DataContext is StackLeaderViewModel);
        Point center = new(portrait.Bounds.Width / 2.0, portrait.Bounds.Height / 2.0);

        window.MouseDown(Assert.NotNull(portrait.TranslatePoint(center, window)), MouseButton.Right);

        await SkipFramesAsync(5);

        DetailsViewModel details = Assert.IsOfType<DetailsViewModel>(gameViewModel.TopScreen);

        Assert.IsOfType<UnitDetailsViewModel>(details.Content);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task DiplomacyTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, new WorldFortificationId("S107FT0000"));
        screen.SelectTabCommand.Execute(screen.Tabs[4]);
        gameViewModel.OpenScreen(screen);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CapitalSpellbookTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, new WorldFortificationId("S107FT0000"));
        screen.SelectTabCommand.Execute(screen.Tabs[3]);
        gameViewModel.OpenScreen(screen);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task DiplomacyEmptyTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players = state.Players;

        foreach (WorldPlayer player in state.Players.Values)
        {
            if (player.Race != WorldRace.Neutral && player.Id != state.TurnInfo.CurrentPlayerId)
            {
                players = players.Remove(player.Id);
            }
        }

        GameModel gameModel = LoadScenario(state with { Players = players });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, new WorldFortificationId("S107FT0000"));
        screen.SelectTabCommand.Execute(screen.Tabs[4]);
        gameViewModel.OpenScreen(screen);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task DiplomacyTradeItemTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, new WorldFortificationId("S107FT0000"));
        screen.SelectTabCommand.Execute(screen.Tabs[4]);
        gameViewModel.OpenScreen(screen);

        DiplomacyTabViewModel diplomacy = Assert.IsOfType<DiplomacyTabViewModel>(screen.Tabs[4]);
        diplomacy.SelectAction(diplomacy.Actions[0]);

        await CaptureAsync(window);

        diplomacy.SelectOffer(diplomacy.Offers[0]);

        await CaptureAsync(window, "price");
    }

    [AvaloniaTest]
    public async Task DiplomacyGiveGoldTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, new WorldFortificationId("S107FT0000"));
        screen.SelectTabCommand.Execute(screen.Tabs[4]);
        gameViewModel.OpenScreen(screen);

        DiplomacyTabViewModel diplomacy = Assert.IsOfType<DiplomacyTabViewModel>(screen.Tabs[4]);
        diplomacy.SelectAction(diplomacy.Actions[1]);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task StackDeadUnitTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit deadSoldier = new(new WorldUnitId("deadSoldier"), soldierDefinition.Id, 0, 0, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (deadSoldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task StackPendingUpgradeTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, soldierDefinition.ExperienceNextLevel - 1, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task StackBlockedUpgradeTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);
        UnitDefinition knightDefinition = scenarioModel.Definitions.GetUnit(humans.Units[1]);
        WorldPlayerBuildingDefinition barracks = TestHelpers.SimpleBuildingDefinition(new WorldPlayerBuildingDefinitionId("barracks"), WorldPlayerBuildingCategory.Unit);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Buildings = new WorldPlayerBuildings([barracks.Id], [])
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, soldierDefinition.ExperienceNextLevel - 1, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(knightDefinition)
            .WithDefinition(barracks)
            .WithUnitUpgrade(new UnitUpgradeDefinition(soldierDefinition.Id, knightDefinition.Id, barracks.Id))
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task StackUnitModifiersTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);
        UnitModifierDefinition boostDefinition = scenarioModel.Definitions.GetUnitModifier(new UnitModifierDefinitionId("g000um2015"));
        UnitModifierDefinition weakenDefinition = scenarioModel.Definitions.GetUnitModifier(new UnitModifierDefinitionId("g000um9002"));

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [new UnitModifier(boostDefinition.Id, RemainingTurns: 3)], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, soldierDefinition.Level, [new UnitModifier(weakenDefinition.Id, RemainingTurns: 2)], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .AddUnitModifier(boostDefinition)
            .AddUnitModifier(weakenDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(boostDefinition)
            .WithDefinition(weakenDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task StackDisbandPromptTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id);
        gameViewModel.OpenScreen(management);

        StackTabViewModel tab = Assert.IsOfType<StackTabViewModel>(management.Tabs[0]);
        UnitGridSlotViewModel slot = tab.UnitGrid.Slots.First(slot => slot.Position == GridPosition.FrontCenter);

        tab.DragDrop.TryBeginDrag(slot.Reference);
        await tab.DragDrop.DropAsync(tab.DismissDropZone.DropReference);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task StackDragTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);
        ItemDefinition potionDefinition = scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0005"));
        ItemDefinition scrollDefinition = scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG5001"));
        SpellItemEffect scrollEffect = Assert.IsOfType<SpellItemEffect>(scrollDefinition.Effect);
        WorldSpellDefinition scrollSpell = scenarioModel.Definitions.GetSpell(scrollEffect.SpellId);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints / 2, 0, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                player.Id,
                new WorldPosition(4, 4),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [
                        (leader, GridPosition.FrontBottom),
                        (soldier, GridPosition.FrontCenter)
                    ]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory(
                [
                    new WorldItem(new WorldItemId("Potion"), potionDefinition.Id, Charges: null),
                    new WorldItem(new WorldItemId("Scroll"), scrollDefinition.Id, Charges: null)
                ])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potionDefinition)
            .WithDefinition(scrollDefinition)
            .WithDefinition(scrollSpell)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id);
        gameViewModel.OpenScreen(management);

        StackTabViewModel tab = Assert.IsOfType<StackTabViewModel>(management.Tabs[0]);

        tab.DragDrop.TryBeginDrag(tab.Inventory.Items[0].Reference);

        await CaptureAsync(window, "potion");

        tab.DragDrop.EndDrag();
        tab.DragDrop.TryBeginDrag(tab.Inventory.Items[1].Reference);

        await CaptureAsync(window, "scroll");

        tab.DragDrop.EndDrag();

        UnitGridSlotViewModel unitSlot = tab.UnitGrid.Slots.First(slot => slot.Position == GridPosition.FrontCenter);
        Button unitButton = window.GetVisualDescendants().OfType<Button>().First(button => Equals(button.DataContext, unitSlot));
        Point unitCenter = Assert.NotNull(unitButton.TranslatePoint(new Point(unitButton.Bounds.Width / 2.0, unitButton.Bounds.Height / 2.0), window));

        window.MouseDown(unitCenter, MouseButton.Left);
        window.MouseMove(unitCenter + new Point(160, 30));

        await CaptureAsync(window, "unit");
    }

    [AvaloniaTest]
    public async Task StackTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, new WorldStackId("S107KC0000")));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task StackItemDetailsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        ItemDefinition bannerDefinition = scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG1002"));
        UnitModifierDefinition bannerModifier = scenarioModel.Definitions.GetUnitModifier(Assert.NotNull(bannerDefinition.EquipmentModifierId));

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, new WorldItem(new WorldItemId("Banner"), bannerDefinition.Id, Charges: null))
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(bannerDefinition)
            .WithDefinition(bannerModifier)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id);
        gameViewModel.OpenScreen(management);

        StackTabViewModel tab = Assert.IsOfType<StackTabViewModel>(management.Tabs[0]);

        gameViewModel.ShowDetails(Assert.NotNull(tab.Equipment.Slots[0].Details));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task StackUnequipDragTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        ItemDefinition bannerDefinition = scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG1002"));
        UnitModifierDefinition bannerModifier = scenarioModel.Definitions.GetUnitModifier(Assert.NotNull(bannerDefinition.EquipmentModifierId));

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, new WorldItem(new WorldItemId("Banner"), bannerDefinition.Id, Charges: null))
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(bannerDefinition)
            .WithDefinition(bannerModifier)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id);
        gameViewModel.OpenScreen(management);

        StackTabViewModel tab = Assert.IsOfType<StackTabViewModel>(management.Tabs[0]);
        tab.DragDrop.TryBeginDrag(tab.Equipment.Slots[0].Reference);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task StackWideTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, 1600, 900);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, new WorldStackId("S107KC0000")));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task StackTabWithoutStackTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit garrisonSoldier = new(new WorldUnitId("garrisonSoldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, soldierDefinition.Level, [], null, false);

        WorldCityDefinition cityDefinition = scenarioModel.Definitions.GetCity(new WorldCityDefinitionId(WorldRace.Human, 1));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonSoldier, GridPosition.FrontBottom, false)
                .Build());

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, city.Id);
        gameViewModel.OpenScreen(management);
        management.SelectNextTabCommand.Execute(null);

        Assert.IsOfType<VisitingStackTabViewModel>(management.SelectedTab);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task StackInventoryOverflowTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);

        ItemDefinition[] itemDefinitions =
        [
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0002")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0004")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0005")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG1001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG2001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG3001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG4001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG7101")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG6001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG7001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG9001"))
        ];

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);

        List<WorldItem> items = [];

        for (int index = 0; index < 40; index++)
        {
            items.Add(new WorldItem(new WorldItemId($"Item{index:D2}"), itemDefinitions[index % itemDefinitions.Length].Id, Charges: null));
        }

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([.. items])
            };

        TestWorldEngineBuilder builder = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace);

        foreach (ItemDefinition itemDefinition in itemDefinitions)
        {
            builder.WithDefinition(itemDefinition);

            if (itemDefinition.EquipmentModifierId is { } modifierId)
            {
                builder.WithDefinition(scenarioModel.Definitions.GetUnitModifier(modifierId));
            }
        }

        (GameSession session, _) = builder.BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id));

        await CaptureAsync(window);

        PagedItemsControl inventory = window.GetVisualDescendants().OfType<PagedItemsControl>().First();
        Button nextButton = inventory.GetVisualDescendants().OfType<Button>().First(button => button.Name == "PART_NextButton");

        Point center = new(nextButton.Bounds.Width / 2.0, nextButton.Bounds.Height / 2.0);
        Point point = Assert.NotNull(nextButton.TranslatePoint(center, window));

        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);

        await CaptureAsync(window, "end");
    }

    [AvaloniaTest]
    public async Task StackLeaderDetailsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, new WorldStackId("S107KC0000")));

        await SkipFramesAsync(5);

        PointerTiltDecorator portrait = window.GetVisualDescendants().OfType<PointerTiltDecorator>().First(candidate => candidate.DataContext is StackLeaderViewModel);
        Point center = new(portrait.Bounds.Width / 2.0, portrait.Bounds.Height / 2.0);

        window.MouseDown(Assert.NotNull(portrait.TranslatePoint(center, window)), MouseButton.Right);

        await SkipFramesAsync(5);

        DetailsViewModel details = Assert.IsOfType<DetailsViewModel>(gameViewModel.TopScreen);

        Assert.IsOfType<UnitDetailsViewModel>(details.Content);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task ExchangeTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);
        UnitDefinition archerDefinition = scenarioModel.Definitions.GetUnit(humans.Units[1]);
        ItemDefinition healingPotionDefinition = scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0001"));
        ItemDefinition strengthPotionDefinition = scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0005"));

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, soldierDefinition.Level, [], null, false);
        WorldUnit archer = new(new WorldUnitId("archer"), archerDefinition.Id, archerDefinition.MaxHitpoints, 0, archerDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .AddUnit(archerDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                player.Id,
                new WorldPosition(4, 4),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [
                        (leader, GridPosition.FrontBottom),
                        (soldier, GridPosition.FrontCenter),
                        (archer, GridPosition.BackCenter)
                    ]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory(
                [
                    new WorldItem(new WorldItemId("HealingPotion"), healingPotionDefinition.Id, Charges: null),
                    new WorldItem(new WorldItemId("StrengthPotion"), strengthPotionDefinition.Id, Charges: null)
                ])
            };

        WorldStack otherStack = stack with
        {
            Id = new WorldStackId("other-stack"),
            Position = new WorldPosition(5, 4),
            Units = TestHelpers.StackUnitGrid(stackDefinitions, [(otherLeader, GridPosition.FrontBottom)]),
            Inventory = new WorldInventory([new WorldItem(new WorldItemId("SecondHealingPotion"), healingPotionDefinition.Id, Charges: null)])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(archerDefinition)
            .WithDefinition(healingPotionDefinition)
            .WithDefinition(strengthPotionDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id);
        screen.SelectTabCommand.Execute(screen.Tabs[1]);
        gameViewModel.OpenScreen(screen);

        await CaptureAsync(window);

        ExchangeTabViewModel tab = Assert.IsOfType<ExchangeTabViewModel>(screen.Tabs[1]);
        UnitGridSlotViewModel source = tab.StackGrid.Slots.First(slot => slot.Position == GridPosition.FrontCenter);
        tab.DragDrop.TryBeginDrag(source.Reference);

        await CaptureAsync(window, "drag-unit");
    }

    [AvaloniaTest]
    public async Task ExchangeInventoryOverflowTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);

        ItemDefinition[] itemDefinitions =
        [
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0002")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0004")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0005")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG1001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG2001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG3001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG4001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG7101")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG6001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG7001")),
            scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG9001"))
        ];

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);

        List<WorldItem> items = [];
        List<WorldItem> otherItems = [];

        for (int index = 0; index < 40; index++)
        {
            items.Add(new WorldItem(new WorldItemId($"Item{index:D2}"), itemDefinitions[index % itemDefinitions.Length].Id, Charges: null));
            otherItems.Add(new WorldItem(new WorldItemId($"OtherItem{index:D2}"), itemDefinitions[(index + 6) % itemDefinitions.Length].Id, Charges: null));
        }

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([.. items])
            };

        WorldStack otherStack = TestHelpers.SimpleStack(
                new WorldStackId("other-stack"),
                player.Id,
                new WorldPosition(5, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(otherLeader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([.. otherItems])
            };

        TestWorldEngineBuilder builder = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace);

        foreach (ItemDefinition itemDefinition in itemDefinitions)
        {
            builder.WithDefinition(itemDefinition);

            if (itemDefinition.EquipmentModifierId is { } modifierId)
            {
                builder.WithDefinition(scenarioModel.Definitions.GetUnitModifier(modifierId));
            }
        }

        (GameSession session, _) = builder.BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id);
        screen.SelectTabCommand.Execute(screen.Tabs[1]);
        gameViewModel.OpenScreen(screen);

        await CaptureAsync(window);

        ExchangeTabViewModel tab = Assert.IsOfType<ExchangeTabViewModel>(screen.Tabs[1]);
        tab.DragDrop.TryBeginDrag(tab.InventoryTransfer.Inventory.Items[0].Reference);

        await CaptureAsync(window, "drag");
    }

    [AvaloniaTest]
    public async Task LeaderAbilitiesTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]) with
        {
            LeaderAbilities = Enum.GetValues<LeaderAbility>().Aggregate(LeaderAbility.None, (all, ability) => all | ability)
        };
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);
        UnitDefinition archerDefinition = scenarioModel.Definitions.GetUnit(humans.Units[1]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, soldierDefinition.Level, [], null, false);
        WorldUnit archer = new(new WorldUnitId("archer"), archerDefinition.Id, archerDefinition.MaxHitpoints, 0, archerDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .AddUnit(archerDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter),
                    (archer, GridPosition.BackCenter)
                ]),
            movementPoints: 20);

        WorldStack otherStack = stack with
        {
            Id = new WorldStackId("other-stack"),
            Position = new WorldPosition(5, 4),
            Units = TestHelpers.StackUnitGrid(stackDefinitions, [(otherLeader, GridPosition.FrontBottom)])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(archerDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id);
        screen.SelectTabCommand.Execute(screen.Tabs[1]);
        gameViewModel.OpenScreen(screen);

        await CaptureAsync(window, "exchange");

        screen.SelectTabCommand.Execute(screen.Tabs[0]);

        await CaptureAsync(window, "stack");
    }

    [AvaloniaTest]
    public async Task MercenaryCampTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldMerchantSite textSource = scenarioModel.Snapshot.WorldState.Sites.Values.OfType<WorldMerchantSite>().First();
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);
        UnitDefinition archerDefinition = scenarioModel.Definitions.GetUnit(humans.Units[1]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);

        WorldMercenarySite site = new(
            new WorldSiteId("camp"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: textSource.TitleTextId,
            GreetingTextId: textSource.GreetingTextId,
            [
                new WorldMercenaryUnit(soldierDefinition.Id, 1, false),
                new WorldMercenaryUnit(archerDefinition.Id, 3, true)
            ],
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            site.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(archerDefinition)
            .WithUnitDynamicUpgrade(scenarioModel.Definitions.GetUnitDynamicUpgrade(archerDefinition.Id))
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForSite(gameModel, gameViewModel, stack.Id);
        gameViewModel.OpenScreen(management);

        await CaptureAsync(window);

        MercenaryTabViewModel mercenaryTab = Assert.IsOfType<MercenaryTabViewModel>(management.Tabs[0]);
        mercenaryTab.Mercenaries[0].SelectCommand.Execute(null);

        await CaptureAsync(window, "selected");

        await Assert.NotNull(mercenaryTab.Offer).ActCommand.ExecuteAsync(null);
        await Assert.NotNull(mercenaryTab.Offer).ActCommand.ExecuteAsync(null);
        await Assert.NotNull(mercenaryTab.Offer).ActCommand.ExecuteAsync(null);

        await CaptureAsync(window, "unaffordable");
    }

    [AvaloniaTest]
    public async Task MerchantShopTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldMerchantSite textSource = scenarioModel.Snapshot.WorldState.Sites.Values.OfType<WorldMerchantSite>().First();
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        ItemDefinition healingPotion = scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0001"));
        ItemDefinition manaPotion = scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0002"));

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);

        WorldMerchantSite site = new(
            new WorldSiteId("shop"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: textSource.TitleTextId,
            GreetingTextId: textSource.GreetingTextId,
            [
                new WorldMerchantItem(healingPotion.Id, 5),
                new WorldMerchantItem(manaPotion.Id, 2)
            ],
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                player.Id,
                site.Entrance,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory(
                [
                    new WorldItem(new WorldItemId("HealingPotion"), healingPotion.Id, Charges: null),
                    new WorldItem(new WorldItemId("ManaPotion"), manaPotion.Id, Charges: null),
                    new WorldItem(new WorldItemId("SecondHealingPotion"), healingPotion.Id, Charges: null)
                ])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(healingPotion)
            .WithDefinition(manaPotion)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForSite(gameModel, gameViewModel, stack.Id);
        gameViewModel.OpenScreen(management);

        await CaptureAsync(window);

        MerchantTabViewModel merchant = Assert.IsOfType<MerchantTabViewModel>(management.Tabs[0]);
        merchant.PurchaseItems[0].SelectCommand.Execute(null);

        await CaptureAsync(window, "selected");

        merchant.SellItems[0].SelectCommand.Execute(null);

        await CaptureAsync(window, "sell-selected");

        merchant.DragDrop.TryBeginDrag(merchant.SellItems[0].Reference);
        await CaptureAsync(window, "drag-sell");
        merchant.DragDrop.EndDrag();

        merchant.DragDrop.TryBeginDrag(merchant.PurchaseItems[1].Reference);
        await CaptureAsync(window, "drag-purchase");
        merchant.DragDrop.EndDrag();
    }

    [AvaloniaTest]
    public async Task MageTowerTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldMageSite textSource = scenarioModel.Snapshot.WorldState.Sites.Values.OfType<WorldMageSite>().First();
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);

        List<KeyValuePair<WorldSpellDefinitionId, WorldResourceSet>> siteSpells =
        [
            .. scenarioModel.Definitions.GetSpellResearchCosts(WorldRace.Human)
                .OrderBy(pair => pair.Key.Value)
                .Take(3)
        ];

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(2000, 2000, 2000, 2000, 2000, 2000)
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);

        WorldMageSite site = new(
            new WorldSiteId("tower"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: textSource.TitleTextId,
            GreetingTextId: textSource.GreetingTextId,
            [.. siteSpells.Select(pair => pair.Key)],
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            site.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        TestWorldEngineBuilder builder = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace);

        foreach ((WorldSpellDefinitionId spellId, WorldResourceSet cost) in siteSpells)
        {
            builder.WithDefinition(scenarioModel.Definitions.GetSpell(spellId));
            builder.WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Human, spellId, cost));
        }

        (GameSession session, _) = builder.BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForSite(gameModel, gameViewModel, stack.Id);
        gameViewModel.OpenScreen(management);

        await CaptureAsync(window);

        MageTabViewModel mage = Assert.IsOfType<MageTabViewModel>(management.Tabs[0]);
        mage.Spells[0].SelectCommand.Execute(null);

        await CaptureAsync(window, "selected");
    }

    [AvaloniaTest]
    public async Task TrainerTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldMerchantSite textSource = scenarioModel.Snapshot.WorldState.Sites.Values.OfType<WorldMerchantSite>().First();
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = scenarioModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitDefinition soldierDefinition = scenarioModel.Definitions.GetUnit(humans.Units[0]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 20, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldTrainerSite site = new(
            new WorldSiteId("trainer"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: textSource.TitleTextId,
            GreetingTextId: textSource.GreetingTextId,
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            site.Entrance,
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        ManagementScreenViewModel management = ManagementScreenViewModel.CreateForSite(gameModel, gameViewModel, stack.Id);
        gameViewModel.OpenScreen(management);

        TrainerTabViewModel trainer = Assert.IsOfType<TrainerTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel slot = trainer.Stack.UnitGrid.Slots.First(slot => slot.Position == GridPosition.FrontCenter);

        await slot.SelectCommand.ExecuteAsync(null);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task ThiefActionTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition undead = scenarioModel.Definitions.GetRace(WorldRace.Undead);
        UnitDefinition thiefDefinition = humans.Leaders
            .Select(scenarioModel.Definitions.GetUnit)
            .First(definition => definition.LeaderCategory == UnitLeaderCategory.Thief);
        UnitDefinition enemyDefinition = scenarioModel.Definitions.GetUnit(undead.Leaders[0]);

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldUnit thief = new(new WorldUnitId("thief"), thiefDefinition.Id, thiefDefinition.MaxHitpoints, 0, thiefDefinition.Level, [], null, false);
        WorldUnit enemyLeader = new(new WorldUnitId("enemyLeader"), enemyDefinition.Id, enemyDefinition.MaxHitpoints, 0, enemyDefinition.Level, [], null, false);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(thief, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            enemy.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(enemyLeader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        TestWorldEngineBuilder builder = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithDefinition(thiefDefinition)
            .WithDefinition(enemyDefinition)
            .WithStack(thiefStack)
            .WithStack(enemyStack)
            .WithSubrace(TestHelpers.NeutralSubrace);

        foreach (WorldThiefActionType type in Enum.GetValues<WorldThiefActionType>())
        {
            builder.WithDefinition(scenarioModel.Definitions.GetThiefAction(type));
        }

        (GameSession session, _) = builder.BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        gameViewModel.OpenScreen(new ThiefActionScreenViewModel(gameModel, gameViewModel, thiefStack.Id, enemyStack.Position));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task ThiefStealTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenarioModel = LoadScenario(null);
        WorldRaceDefinition humans = scenarioModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition thiefDefinition = humans.Leaders
            .Select(scenarioModel.Definitions.GetUnit)
            .First(definition => definition.LeaderCategory == UnitLeaderCategory.Thief);
        ItemDefinition healingPotion = scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0001"));
        ItemDefinition manaPotion = scenarioModel.Definitions.GetItem(new ItemDefinitionId("G000IG0002"));

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldUnit thief = new(new WorldUnitId("thief"), thiefDefinition.Id, thiefDefinition.MaxHitpoints, 0, thiefDefinition.Level, [], null, false);

        WorldMerchantSite site = new(
            new WorldSiteId("shop"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [
                new WorldMerchantItem(healingPotion.Id, 5),
                new WorldMerchantItem(manaPotion.Id, 2)
            ],
            VisitorIds: [],
            AiPriority: 3);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(thief, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        TestWorldEngineBuilder builder = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(thiefDefinition)
            .WithDefinition(healingPotion)
            .WithDefinition(manaPotion)
            .WithStack(thiefStack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace);

        foreach (WorldThiefActionType type in Enum.GetValues<WorldThiefActionType>())
        {
            builder.WithDefinition(scenarioModel.Definitions.GetThiefAction(type));
        }

        (GameSession session, _) = builder.BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, scenarioModel.Texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        ThiefActionScreenViewModel actions = new(gameModel, gameViewModel, thiefStack.Id, site.Entrance);
        gameViewModel.OpenScreen(actions);

        await actions.SelectActionAsync(actions.Actions[0]);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<ThiefStealScreenViewModel>());

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task UnitPurchaseTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ManagementScreenViewModel management = new(
            gameModel,
            gameViewModel,
            ScreenMusic.Keep,
            null,
            screen => [new CityTabViewModel(gameModel, gameViewModel, screen, new WorldFortificationId("S107FT0000"))]);

        CityTabViewModel city = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(city);

        UnitGridSlotViewModel emptySlot = city.GarrisonGrid.Slots.First(slot => !slot.HasUnit && slot.IsVisible);
        await emptySlot.SelectCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<UnitPurchaseViewModel>());

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task LeaderPurchaseTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldCapital capital = gameModel.Snapshot.WorldState.Fortifications.Values.OfType<WorldCapital>().First();
        WorldPlan plan = WorldPlan.For(gameModel.World.State);
        WorldStack stack = Assert.NotNull(plan.GetStackAt(capital.Entrance));
        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(stack.Id, new WorldPosition(11, 14)));

        ManagementScreenViewModel management = new(
            gameModel,
            gameViewModel,
            ScreenMusic.Keep,
            null,
            screen => [new CityTabViewModel(gameModel, gameViewModel, screen, capital.Id)]);

        CityTabViewModel city = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(city);

        Assert.That(city.HasStack, Is.False);

        city.LeaderBlankslate.HireCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<LeaderPurchaseViewModel>());

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task LeaderUpgradeTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        WorldStackId stackId = new("S107KC0000");
        WorldStack stack = state.Stacks[stackId];
        (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(scenario.Definitions);

        WorldStack upgradable = stack with
        {
            Units = stack.Units.Replace(leader, leader with { Level = 2 }),
            PendingLeaderUpgrades = 1
        };

        GameModel gameModel = LoadScenario(state with { Stacks = state.Stacks.SetItem(stackId, upgradable) });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        LeaderUpgradeViewModel upgrades = new(gameModel, gameViewModel, stackId);
        gameViewModel.OpenScreen(upgrades);

        await CaptureAsync(window);

        upgrades.Options[0].SelectCommand.Execute(null);

        await CaptureAsync(window, "selected");
    }

    [AvaloniaTest]
    public async Task LeaderExportTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        LeaderExportScreenViewModel screen = new(gameModel, gameViewModel);
        screen.Items[1].AddCommand.Execute(null);
        screen.Items[1].AddCommand.Execute(null);
        gameViewModel.OpenScreen(screen);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CapitalSceneBuildingsTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Buildings = new WorldPlayerBuildings(
                [],
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0001"),
                    new WorldPlayerBuildingDefinitionId("G000BB0002"),
                    new WorldPlayerBuildingDefinitionId("G000BB0003"),
                    new WorldPlayerBuildingDefinitionId("G000BB0004"),
                    new WorldPlayerBuildingDefinitionId("G000BB0005"),
                    new WorldPlayerBuildingDefinitionId("G000BB0006"),
                    new WorldPlayerBuildingDefinitionId("G000BB0007"),
                    new WorldPlayerBuildingDefinitionId("G000BB0008"),
                    new WorldPlayerBuildingDefinitionId("G000BB0009"),
                    new WorldPlayerBuildingDefinitionId("G000BB0010"),
                    new WorldPlayerBuildingDefinitionId("G000BB0011"),
                    new WorldPlayerBuildingDefinitionId("G000BB0012"),
                    new WorldPlayerBuildingDefinitionId("G000BB0013"),
                    new WorldPlayerBuildingDefinitionId("G000BB0014"),
                    new WorldPlayerBuildingDefinitionId("G000BB0015"),
                    new WorldPlayerBuildingDefinitionId("G000BB0016"),
                    new WorldPlayerBuildingDefinitionId("G000BB0017"),
                    new WorldPlayerBuildingDefinitionId("G000BB0018"),
                    new WorldPlayerBuildingDefinitionId("G000BB0019"),
                    new WorldPlayerBuildingDefinitionId("G000BB0020"),
                    new WorldPlayerBuildingDefinitionId("G000BB0021"),
                    new WorldPlayerBuildingDefinitionId("G000BB0022"),
                    new WorldPlayerBuildingDefinitionId("G000BB0023"),
                    new WorldPlayerBuildingDefinitionId("G000BB0024"),
                    new WorldPlayerBuildingDefinitionId("G000BB0025")
                ])
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [],
            Units: [],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        TestWorldEngineBuilder builder = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(raceDefinition)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace);

        foreach (WorldPlayerBuildingDefinitionId buildingId in player.Buildings.Acquired)
        {
            builder.WithDefinition(MakeBuildingDefinition(buildingId));
        }

        (GameSession session, _) = builder.BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);
        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, capital.Id));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CapitalBuildingsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(new ManagementScreenViewModel(
            gameModel,
            gameViewModel,
            ScreenMusic.Keep,
            null,
            screen => [new BuildingTabViewModel(gameModel, gameViewModel, screen)]));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CapitalBuildingsTreeTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        WorldPlayerId playerId = state.TurnInfo.CurrentPlayerId;
        WorldPlayer player = state.Players[playerId];

        WorldPlayer expanded = player with
        {
            Resources = player.Resources with { Gold = 5000 },
            Buildings = new WorldPlayerBuildings(
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0001"),
                    new WorldPlayerBuildingDefinitionId("G000BB0002"),
                    new WorldPlayerBuildingDefinitionId("G000BB0003"),
                    new WorldPlayerBuildingDefinitionId("G000BB0004"),
                    new WorldPlayerBuildingDefinitionId("G000BB0005"),
                    new WorldPlayerBuildingDefinitionId("G000BB0006"),
                    new WorldPlayerBuildingDefinitionId("G000BB0007"),
                    new WorldPlayerBuildingDefinitionId("G000BB0008"),
                    new WorldPlayerBuildingDefinitionId("G000BB0009"),
                    new WorldPlayerBuildingDefinitionId("G000BB0010"),
                    new WorldPlayerBuildingDefinitionId("G000BB0011"),
                    new WorldPlayerBuildingDefinitionId("G000BB0012"),
                    new WorldPlayerBuildingDefinitionId("G000BB0013"),
                    new WorldPlayerBuildingDefinitionId("G000BB0014"),
                    new WorldPlayerBuildingDefinitionId("G000BB0015"),
                    new WorldPlayerBuildingDefinitionId("G000BB0016"),
                    new WorldPlayerBuildingDefinitionId("G000BB0017"),
                    new WorldPlayerBuildingDefinitionId("G000BB0018"),
                    new WorldPlayerBuildingDefinitionId("G000BB0019"),
                    new WorldPlayerBuildingDefinitionId("G000BB0020"),
                    new WorldPlayerBuildingDefinitionId("G000BB0021"),
                    new WorldPlayerBuildingDefinitionId("G000BB0022"),
                    new WorldPlayerBuildingDefinitionId("G000BB0023"),
                    new WorldPlayerBuildingDefinitionId("G000BB0024"),
                    new WorldPlayerBuildingDefinitionId("G000BB0025")
                ],
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0001"),
                    new WorldPlayerBuildingDefinitionId("G000BB0002"),
                    new WorldPlayerBuildingDefinitionId("G000BB0010"),
                    new WorldPlayerBuildingDefinitionId("G000BB0012"),
                    new WorldPlayerBuildingDefinitionId("G000BB0013"),
                    new WorldPlayerBuildingDefinitionId("G000BB0023")
                ])
        };

        GameModel gameModel = LoadScenario(state with { Players = state.Players.SetItem(playerId, expanded) });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(new ManagementScreenViewModel(gameModel, gameViewModel, ScreenMusic.Keep, null, screen => [new BuildingTabViewModel(gameModel, gameViewModel, screen)]));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task CapitalBuildingsTreeSelectedTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        WorldPlayerId playerId = state.TurnInfo.CurrentPlayerId;
        WorldPlayer player = state.Players[playerId];

        WorldPlayer expanded = player with
        {
            Resources = player.Resources with { Gold = 5000 },
            Buildings = new WorldPlayerBuildings(
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0001"),
                    new WorldPlayerBuildingDefinitionId("G000BB0002"),
                    new WorldPlayerBuildingDefinitionId("G000BB0003"),
                    new WorldPlayerBuildingDefinitionId("G000BB0004"),
                    new WorldPlayerBuildingDefinitionId("G000BB0005"),
                    new WorldPlayerBuildingDefinitionId("G000BB0006"),
                    new WorldPlayerBuildingDefinitionId("G000BB0007"),
                    new WorldPlayerBuildingDefinitionId("G000BB0008"),
                    new WorldPlayerBuildingDefinitionId("G000BB0009"),
                    new WorldPlayerBuildingDefinitionId("G000BB0010"),
                    new WorldPlayerBuildingDefinitionId("G000BB0011"),
                    new WorldPlayerBuildingDefinitionId("G000BB0012"),
                    new WorldPlayerBuildingDefinitionId("G000BB0013"),
                    new WorldPlayerBuildingDefinitionId("G000BB0014"),
                    new WorldPlayerBuildingDefinitionId("G000BB0015"),
                    new WorldPlayerBuildingDefinitionId("G000BB0016"),
                    new WorldPlayerBuildingDefinitionId("G000BB0017"),
                    new WorldPlayerBuildingDefinitionId("G000BB0018"),
                    new WorldPlayerBuildingDefinitionId("G000BB0019"),
                    new WorldPlayerBuildingDefinitionId("G000BB0020"),
                    new WorldPlayerBuildingDefinitionId("G000BB0021"),
                    new WorldPlayerBuildingDefinitionId("G000BB0022"),
                    new WorldPlayerBuildingDefinitionId("G000BB0023"),
                    new WorldPlayerBuildingDefinitionId("G000BB0024"),
                    new WorldPlayerBuildingDefinitionId("G000BB0025")
                ],
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0001"),
                    new WorldPlayerBuildingDefinitionId("G000BB0002"),
                    new WorldPlayerBuildingDefinitionId("G000BB0010"),
                    new WorldPlayerBuildingDefinitionId("G000BB0012"),
                    new WorldPlayerBuildingDefinitionId("G000BB0013"),
                    new WorldPlayerBuildingDefinitionId("G000BB0023")
                ])
        };

        GameModel gameModel = LoadScenario(state with { Players = state.Players.SetItem(playerId, expanded) });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, screen => [new BuildingTabViewModel(gameModel, gameViewModel, screen)]);
        gameViewModel.OpenScreen(management);

        await SkipFramesAsync(5);

        BuildingTabViewModel tab = Assert.IsOfType<BuildingTabViewModel>(management.Tabs[0]);
        BuildingNodeViewModel shrine = tab.Graph.Nodes.First(node => node.BuildingId == new WorldPlayerBuildingDefinitionId("G000BB0003"));

        tab.SelectBuildingCommand.Execute(shrine);

        await CaptureAsync(window);

        BuildingTabView view = window.GetVisualDescendants().OfType<BuildingTabView>().First();
        Button button = view.GetVisualDescendants().OfType<Button>().First(candidate => ReferenceEquals(candidate.DataContext, shrine));
        Point center = new(button.Bounds.Width / 2.0, button.Bounds.Height / 2.0);

        window.MouseDown(Assert.NotNull(button.TranslatePoint(center, window)), MouseButton.Right);

        await SkipFramesAsync(5);

        Assert.IsOfType<DetailsViewModel>(gameViewModel.TopScreen);

        await CaptureAsync(window, "unit-details");

        window.MouseUp(Assert.NotNull(button.TranslatePoint(center, window)), MouseButton.Right);

        await SkipFramesAsync(5);

        Assert.That(gameViewModel.TopScreen, Is.Not.InstanceOf<DetailsViewModel>());

        tab.ClearSelectionCommand.Execute(null);

        await SkipFramesAsync(5);

        BuildingNodeViewModel temple = tab.Graph.Nodes.First(node => node.BuildingId == new WorldPlayerBuildingDefinitionId("G000BB0023"));
        Button templeButton = view.GetVisualDescendants().OfType<Button>().First(candidate => ReferenceEquals(candidate.DataContext, temple));
        Point templeCenter = new(templeButton.Bounds.Width / 2.0, templeButton.Bounds.Height / 2.0);

        window.MouseDown(Assert.NotNull(templeButton.TranslatePoint(templeCenter, window)), MouseButton.Right);

        await SkipFramesAsync(5);

        DetailsViewModel buildingDetails = Assert.IsOfType<DetailsViewModel>(gameViewModel.TopScreen);

        Assert.IsOfType<BuildingDetailsViewModel>(buildingDetails.Content);

        await CaptureAsync(window, "building-details");

        window.MouseUp(Assert.NotNull(templeButton.TranslatePoint(templeCenter, window)), MouseButton.Right);

        await SkipFramesAsync(5);

        tab.SelectBuildingCommand.Execute(shrine);

        await SkipFramesAsync(5);

        StackPanel unit = window.GetVisualDescendants().OfType<StackPanel>().First(candidate => candidate.DataContext is BuildingUnitViewModel);
        Point unitCenter = new(unit.Bounds.Width / 2.0, unit.Bounds.Height / 2.0);

        window.MouseDown(Assert.NotNull(unit.TranslatePoint(unitCenter, window)), MouseButton.Right);

        await SkipFramesAsync(5);

        DetailsViewModel unitDetails = Assert.IsOfType<DetailsViewModel>(gameViewModel.TopScreen);

        Assert.IsOfType<UnitDetailsViewModel>(unitDetails.Content);

        await CaptureAsync(window, "unlocked-unit-details");
    }

    [AvaloniaTest]
    public async Task SpellbookTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        WorldPlayerId playerId = state.TurnInfo.CurrentPlayerId;
        WorldPlayer player = state.Players[playerId];

        WorldPlayer learned = player with
        {
            Resources = player.Resources with { LifeMana = 300 },
            Spellbook = player.Spellbook with
            {
                Acquired =
                [
                    player.Spellbook.Available[0],
                    player.Spellbook.Available[1],
                    new WorldSpellDefinitionId("g000ss0015"),
                    new WorldSpellDefinitionId("g000ss0018")
                ]
            }
        };

        GameModel gameModel = LoadScenario(state with { Players = state.Players.SetItem(playerId, learned) });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldPlayerId currentPlayer = gameModel.Snapshot.WorldState.TurnInfo.CurrentPlayerId;

        await gameModel.World.DispatchCommandAsync(new WorldConstructBuildingCommand(currentPlayer, new WorldPlayerBuildingDefinitionId("g000bb0024")));

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);
        gameViewModel.OpenScreen(screen);

        await CaptureAsync(window);

        SpellbookTabView view = window.GetVisualDescendants().OfType<SpellbookTabView>().First();
        ScrollViewer scrollViewer = view.GetVisualDescendants().OfType<ScrollViewer>().First();
        scrollViewer.ScrollToEnd();

        await CaptureAsync(window, "scrolled");

        scrollViewer.ScrollToHome();

        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);
        spellbook.SelectSpellCommand.Execute(spellbook.Levels[0].Spells[0]);

        await CaptureAsync(window, "known-details");

        spellbook.SelectSpellCommand.Execute(spellbook.Levels[1].Spells[4]);

        await CaptureAsync(window, "research-details");
    }

    [AvaloniaTest]
    public async Task SpellbookWithoutBuildingTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task QuestTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel scenario = LoadScenario(null);

        WorldState state = scenario.World.State;
        WorldPlayerId playerId = state.TurnInfo.CurrentPlayerId;
        GameModel gameModel = LoadScenario(state with
        {
            TurnInfo = state.TurnInfo with { TurnNumber = 18 },
            ScenarioState = state.ScenarioState with
            {
                Messages =
                [
                    new WorldScenarioMessage(1, new ScenarioEventId("S107EV0000"), new ScenarioTextId("S107EV0000:POPUP_TXT:0"), new ScenarioEventPortraitId("G000UU5001"), PortraitName: null, IsPortraitLeft: true, playerId, [playerId]),
                    new WorldScenarioMessage(4, new ScenarioEventId("S107EV0006"), new ScenarioTextId("S107EV0006:POPUP_TXT:0"), new ScenarioEventPortraitId("G000UU0020"), PortraitName: null, IsPortraitLeft: true, playerId, [playerId]),
                    new WorldScenarioMessage(9, new ScenarioEventId("S107EV001a"), new ScenarioTextId("S107EV001a:POPUP_TXT:0"), new ScenarioEventPortraitId("G000UU0070"), PortraitName: null, IsPortraitLeft: true, playerId, [playerId]),
                    new WorldScenarioMessage(9, new ScenarioEventId("S107EV001a"), new ScenarioTextId("S107EV001a:POPUP_TXT:1"), new ScenarioEventPortraitId("G000UU0021"), PortraitName: null, IsPortraitLeft: false, playerId, [playerId]),
                    new WorldScenarioMessage(9, new ScenarioEventId("S107EV001a"), new ScenarioTextId("S107EV001a:POPUP_TXT:2"), new ScenarioEventPortraitId("G000UU0070"), PortraitName: null, IsPortraitLeft: true, playerId, [playerId]),
                    new WorldScenarioMessage(12, new ScenarioEventId("S107EV0014"), new ScenarioTextId("S107EV0014:POPUP_TXT:0"), PortraitId: null, PortraitName: null, IsPortraitLeft: true, playerId, [playerId]),
                    new WorldScenarioMessage(17, new ScenarioEventId("S107EV0019"), new ScenarioTextId("S107EV0019:POPUP_TXT:0"), new ScenarioEventPortraitId("G000UU0020"), PortraitName: null, IsPortraitLeft: true, playerId, [playerId])
                ]
            }
        });

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);

        await WaitForWorldAsync(window);

        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForQuest(gameModel, gameViewModel));

        await CaptureAsync(window);

        QuestTabView view = window.GetVisualDescendants().OfType<QuestTabView>().First();
        ScrollViewer scrollViewer = view.GetVisualDescendants().OfType<ScrollViewer>().First();
        scrollViewer.ScrollToHome();

        await CaptureAsync(window, "top");
    }

    [AvaloniaTest]
    public async Task DetailsCardsTest()
    {
        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition humans = gameModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = gameModel.Definitions.GetUnit(humans.Leaders[0]);
        UnitModifierDefinition boostDefinition = gameModel.Definitions.GetUnitModifier(new UnitModifierDefinitionId("g000um2015"));

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints - 40, 35, leaderDefinition.Level, [new UnitModifier(boostDefinition.Id, RemainingTurns: 3)], null, false);

        UnitDetailsViewModel unitDetails = UnitDetailsViewModel.CreateFromWorld(leader, null, gameModel.Definitions, gameModel.Texts, gameModel.Stats);
        ItemDetailsViewModel bannerDetails = ItemDetailsViewModel.Create(
            new ItemDefinitionId("G000IG1002"),
            charges: null,
            WorldResourceSet.Empty,
            gameModel.Definitions,
            gameModel.Texts);
        ItemDetailsViewModel staffDetails = ItemDetailsViewModel.Create(
            new ItemDefinitionId("G000IG6001"),
            charges: null,
            WorldResourceSet.Empty,
            gameModel.Definitions,
            gameModel.Texts);

        StackPanel items = new()
        {
            Spacing = 40,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new ContentControl { Content = bannerDetails, ClipToBounds = false },
                new ContentControl { Content = staffDetails, ClipToBounds = false },
                new ToolTip { Content = "Ability lvl 3 required to unlock this glyph", Opacity = 1.0, HorizontalAlignment = HorizontalAlignment.Left }
            }
        };

        Window window = new()
        {
            WindowState = WindowState.Normal,
            SizeToContent = SizeToContent.Manual,
            Width = BaseWidth,
            Height = BaseHeight,
            Background = new SolidColorBrush(Theme.Common.WindowBackgroundColor),
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new ContentControl { Content = unitDetails, ClipToBounds = false, VerticalAlignment = VerticalAlignment.Center },
                    items
                }
            }
        };

        AssetScope.SetAssets(window, assets);
        RegisterWindow(window);
        window.Show();

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task DetailsUnitUpgradeBlockedTest()
    {
        GameModel gameModel = LoadScenario(null);
        UnitDefinition squireDefinition = gameModel.Definitions.GetUnit(new UnitDefinitionId("G000UU0001"));
        WorldUnit squire = new(new WorldUnitId("squire"), squireDefinition.Id, squireDefinition.MaxHitpoints, 70, squireDefinition.Level, [], null, false);
        GameSnapshot snapshot = gameModel.Snapshot;
        WorldPlayer owner = snapshot.WorldState.Players[snapshot.ViewPlayerId] with { Buildings = new WorldPlayerBuildings([], []) };
        UnitDetailsViewModel unitDetails = UnitDetailsViewModel.CreateFromWorld(squire, owner, gameModel.Definitions, gameModel.Texts, gameModel.Stats);

        Window window = new()
        {
            WindowState = WindowState.Normal,
            SizeToContent = SizeToContent.Manual,
            Width = BaseWidth,
            Height = BaseHeight,
            Background = new SolidColorBrush(Theme.Common.WindowBackgroundColor),
            Content = new ContentControl
            {
                Content = unitDetails,
                ClipToBounds = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        AssetScope.SetAssets(window, assets);
        RegisterWindow(window);
        window.Show();

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task DetailsUpgradedLeaderTest()
    {
        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition humans = gameModel.Definitions.GetRace(WorldRace.Human);
        UnitDefinition leaderDefinition = gameModel.Definitions.GetUnit(humans.Leaders[0]);

        WorldUnit leader = new(
            new WorldUnitId("leader"),
            leaderDefinition.Id,
            leaderDefinition.MaxHitpoints,
            0,
            15,
            [
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4001")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4004")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4006")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4007")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4009")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4011")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4013")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4014")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4101")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4017")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4018")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4102")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4020")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um4021")),
                UnitModifier.FromEquipment(new UnitModifierDefinitionId("g000um2101")),
                new UnitModifier(new UnitModifierDefinitionId("g000um2015"), RemainingTurns: 3)
            ],
            null,
            false);

        GameSnapshot snapshot = gameModel.Snapshot;
        WorldPlayer owner = snapshot.WorldState.Players[snapshot.ViewPlayerId];
        UnitDetailsViewModel unitDetails = UnitDetailsViewModel.CreateFromWorld(leader, owner, gameModel.Definitions, gameModel.Texts, gameModel.Stats);

        Window window = new()
        {
            WindowState = WindowState.Normal,
            SizeToContent = SizeToContent.Manual,
            Width = BaseWidth,
            Height = BaseHeight,
            Background = new SolidColorBrush(Theme.Common.WindowBackgroundColor),
            Content = new ContentControl
            {
                Content = unitDetails,
                ClipToBounds = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        AssetScope.SetAssets(window, assets);
        RegisterWindow(window);
        window.Show();

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task DetailsSpellTest()
    {
        GameModel gameModel = LoadScenario(null);
        SpellDetailsViewModel spellDetails = SpellDetailsViewModel.Create(new WorldSpellDefinitionId("G000SS0008"), gameModel.Definitions, gameModel.Texts);

        Window window = new()
        {
            WindowState = WindowState.Normal,
            SizeToContent = SizeToContent.Manual,
            Width = BaseWidth,
            Height = BaseHeight,
            Background = new SolidColorBrush(Theme.Common.WindowBackgroundColor),
            Content = new ContentControl
            {
                Content = spellDetails,
                ClipToBounds = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        AssetScope.SetAssets(window, assets);
        RegisterWindow(window);
        window.Show();

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task TooltipTest()
    {
        StackPanel darkTooltips = new()
        {
            Spacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new ToolTip { Content = "Gold", Opacity = 1.0, HorizontalAlignment = HorizontalAlignment.Left },
                new ToolTip { Content = "Ability lvl 3 required to unlock this glyph", Opacity = 1.0, HorizontalAlignment = HorizontalAlignment.Left }
            }
        };

        StackPanel panelTooltips = new()
        {
            Spacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new ToolTip { Content = "Imperial Knight", Opacity = 1.0, HorizontalAlignment = HorizontalAlignment.Left },
                new ToolTip { Content = "Ability lvl 3 required to unlock this glyph", Opacity = 1.0, HorizontalAlignment = HorizontalAlignment.Left }
            }
        };

        Panel panel = new()
        {
            Background = new SolidColorBrush(Theme.Panel.TranslucentBackgroundColor),
            Children = { panelTooltips }
        };

        Grid.SetColumn(panel, 1);

        Window window = new()
        {
            WindowState = WindowState.Normal,
            SizeToContent = SizeToContent.Manual,
            Width = 640,
            Height = 260,
            Background = new SolidColorBrush(Theme.Common.WindowBackgroundColor),
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                Children = { darkTooltips, panel }
            }
        };

        RegisterWindow(window);
        window.Show();

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task MedallionTest()
    {
        StackPanel row = new() { Orientation = Orientation.Horizontal, Spacing = 40, Margin = new Thickness(40) };

        foreach ((string classes, bool spinning) in new[] { ("world-medallion", false), ("world-medallion lit", false), ("world-medallion", false), ("world-medallion", true) })
        {
            Button button = new()
            {
                Content = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new EndTurnIconDecorator { IsSpinning = spinning },
                        new TextBlock { Text = "End turn" }
                    }
                }
            };

            button.Classes.AddRange(classes.Split(' '));
            row.Children.Add(button);
        }

        Window window = new()
        {
            WindowState = WindowState.Normal,
            SizeToContent = SizeToContent.Manual,
            Width = 960,
            Height = 260,
            Background = new SolidColorBrush(Theme.Common.WindowBackgroundColor),
            Content = row
        };

        window.Styles.Add(new StyleInclude(new Uri("avares://Gallean.Client/")) { Source = new Uri("avares://Gallean.Client/Screens/World/WorldChrome.axaml") });
        RegisterWindow(window);
        window.Show();

        Button hovered = Assert.IsOfType<Button>(row.Children[2]);
        window.MouseMove(Assert.NotNull(hovered.TranslatePoint(new Point(90, 90), window)));
        await SkipFramesAsync(10);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task PriceButtonTest()
    {
        WorldResourceSet resources = new(1000, 1000, 100, 1000, 1000, 1000);
        WorldResourceSet[] prices =
        [
            new(200, 0, 0, 0, 0, 0),
            new(0, 550, 300, 0, 0, 0),
            new(0, 550, 300, 400, 0, 0),
            new(0, 550, 300, 400, 1250, 0),
            new(1500, 550, 300, 400, 1250, 0)
        ];

        StackPanel panel = new()
        {
            Width = 304,
            Margin = new Thickness(18),
            Spacing = 14,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        foreach (WorldResourceSet price in prices)
        {
            panel.Children.Add(new PriceButton
            {
                Classes = { "pill" },
                Content = "Research",
                Status = new OfferStatusViewModel(new OfferPriceViewModel(price, resources), null)
            });
        }

        Window window = new()
        {
            Width = 340,
            Height = 380,
            Background = Brushes.Black,
            Content = panel
        };

        RegisterWindow(window);
        window.Show();

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task AlertErrorTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, new WorldFortificationId("S107FT0000")));
        gameViewModel.ShowError("The party does not have enough leadership");

        await SkipFramesAsync(40);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task AlertInfoTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        gameViewModel.OpenScreen(ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, new WorldFortificationId("S107FT0000")));
        gameViewModel.ShowInfo(IconMoniker.Spellbook, "Item used", "Scroll of Healing");

        await SkipFramesAsync(40);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new BattleScreenViewModel(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0)));

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleHoverTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));

        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        BattleState state = Assert.NotNull(gameModel.Snapshot.BattleState);

        foreach ((BattleUnit unit, GridPosition position) in state.DefenderGrid.GetUnits())
        {
            if (unit.Id == new BattleUnitId(4))
            {
                viewModel.Hover(new BattleGridTile(BattleSide.Defender, position));
            }
        }

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleRenderResolutionTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.UpdateSettings(GameSettings.Create(100, 100, false, 50, 5, 10, BattleSpeed.Normal, null, true));
        gameViewModel.ShowScreen(new BattleScreenViewModel(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0)));

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattlePortraitsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        BattleUnit wounded = MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel);
        BattleUnit critical = MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel);
        BattleUnit dead = MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel);
        BattleUnit scratched = MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel);

        BattleUnit paralyzed = MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel);
        BattleUnit petrified = MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel);

        builder.Add(paralyzed with { StatusEffects = [new BattleParalyzeStatusEffect(RemainingTurns: 2)] });
        builder.Add(wounded with { Hitpoints = wounded.Hitpoints / 2, StatusEffects = [new BattlePoisonStatusEffect(Damage: 10, RemainingTurns: 2)] });
        builder.Add(critical with { Hitpoints = critical.Hitpoints / 5 });

        builder.Add(petrified with { StatusEffects = [new BattlePetrifyStatusEffect(RemainingTurns: 2)] });
        builder.Add(scratched with { Hitpoints = scratched.Hitpoints * 4 / 5, StatusEffects = [new BattleFrostbiteStatusEffect(Damage: 10, RemainingTurns: 2)] });
        builder.Add(dead with { Hitpoints = 0 });

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new BattleScreenViewModel(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0)));

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleHereticCapitalTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldState world = gameModel.Snapshot.WorldState;
        WorldCapital capital = world.Fortifications.Values.OfType<WorldCapital>().First(candidate => world.Players[candidate.OwnerId].Race == WorldRace.Heretic);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Heretic);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                capital.Footprint.TopLeft));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new BattleScreenViewModel(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0)));

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleAuraTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, new UnitDefinitionId("G000UU0022"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, new UnitDefinitionId("G000UU0009"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, new UnitDefinitionId("G000UU0010"), BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, new UnitDefinitionId("G000UU0099"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, new UnitDefinitionId("G000UU0096"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(5, new UnitDefinitionId("G000UU0098"), BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new BattleScreenViewModel(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0)));

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleOutcomeTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        await battle.DispatchCommandAsync(new BattleAutoResolveCommand());

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new BattleScreenViewModel(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0)));

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleLogTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        await battle.DispatchCommandAsync(new BattleAutoResolveCommand());

        viewModel.OpenLogCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<BattleLogPopupViewModel>());

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleLogEmptyTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        viewModel.OpenLogCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<BattleLogPopupViewModel>());

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleUnitDetailsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        viewModel.ShowUnitDetails(new BattleUnitId(3));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleUnitDetailsPendingUpgradeTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);
        UnitDefinition inspected = gameModel.Definitions.GetUnit(defenders.Units[0]);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(2, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(3, defenders.Units[0], BattleSide.Defender, gameModel) with { Experience = inspected.ExperienceNextLevel - 1 });

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        viewModel.ShowUnitDetails(new BattleUnitId(3));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleItemsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel), new GridPosition(0, 1));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel), new GridPosition(0, 0));
        builder.Add(MakeBattleUnit(2, defenders.Units[0], BattleSide.Defender, gameModel), new GridPosition(0, 1));
        builder.AddItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, new ItemDefinitionId("G000IG0005"), Charges: null, Consumed: false));
        builder.AddItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle2, new ItemDefinitionId("G000IG0006"), Charges: null, Consumed: false));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleDefenderItemsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Undead);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Human);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Units[0], BattleSide.Attacker, gameModel), new GridPosition(0, 1));
        builder.Add(MakeBattleUnit(1, defenders.Leaders[0], BattleSide.Defender, gameModel), new GridPosition(0, 1));
        builder.AddItem(new BattleItem(BattleSide.Defender, LeaderEquipmentSlot.Battle1, new ItemDefinitionId("G000IG0005"), Charges: null, Consumed: false));
        builder.AddItem(new BattleItem(BattleSide.Defender, LeaderEquipmentSlot.Battle2, new ItemDefinitionId("G000IG0006"), Charges: null, Consumed: false));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleTargetTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel), new GridPosition(0, 1));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel), new GridPosition(0, 0));
        builder.Add(MakeBattleUnit(2, defenders.Units[0], BattleSide.Defender, gameModel) with { Hitpoints = 40 }, new GridPosition(0, 1));
        builder.AddItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, new ItemDefinitionId("G000IG0005"), Charges: null, Consumed: false));
        builder.AddItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle2, new ItemDefinitionId("G000IG0006"), Charges: null, Consumed: false));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, new GridPosition(0, 1)));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleLargeUnitsTest()
    {
        using Sandbox sb = Sandbox.Create();

        UnitDefinitionId manticore = new("g000uu6016");

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, manticore, BattleSide.Attacker, gameModel), new GridPosition(0, 0));

        builder.Add(MakeBattleUnit(1, manticore, BattleSide.Defender, gameModel), new GridPosition(0, 1));
        builder.Add(MakeBattleUnit(2, defenders.Units[0], BattleSide.Defender, gameModel), new GridPosition(0, 2));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        viewModel.Hover(new BattleGridTile(BattleSide.Attacker, new GridPosition(0, 0)));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleDeadUnitsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel) with { Hitpoints = 0 });
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel) with { Hitpoints = 0 });
        builder.Add(MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new BattleScreenViewModel(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0)));

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleStatusEffectsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel) with
        {
            StatusEffects =
            [
                new BattleDefendStatusEffect(),
                new BattleIncreaseDamageStatusEffect(Percent: 25, IsInfinite: false),
                new BattleFortificationStatusEffect(Armor: 15)
            ]
        });
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel) with
        {
            StatusEffects =
            [
                new BattlePoisonStatusEffect(RemainingTurns: 2, Damage: 5),
                new BattleFrostbiteStatusEffect(RemainingTurns: 3, Damage: 4),
                new BattleBlisterStatusEffect(RemainingTurns: 1, Damage: 6),
                new BattleShatterArmorStatusEffect(Armor: 20),
                new BattleReduceInitiativeStatusEffect(Percent: 50, IsInfinite: false)
            ]
        });
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel) with
        {
            StatusEffects = [new BattleRetreatStatusEffect()]
        });

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel) with
        {
            StatusEffects =
            [
                new BattleFrostbiteStatusEffect(RemainingTurns: 1, Damage: 3),
                new BattleShatterArmorStatusEffect(Armor: 20),
                new BattleParalyzeStatusEffect(RemainingTurns: 1)
            ]
        });
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel) with
        {
            StatusEffects = [new BattlePetrifyStatusEffect(RemainingTurns: 2)]
        });
        builder.Add(MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel) with
        {
            StatusEffects =
            [
                new BattleReduceDamageStatusEffect(Percent: 50, IsInfinite: true),
                new BattleCureStatusEffect(),
                new BattleTransformStatusEffect(BattleTransformType.LevelDrain, defenders.Units[1], RestoreLevel: 2, RestoreWards: [], RemainingTurns: 3)
            ]
        });

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        await CaptureAsync(window);

        viewModel.ShowUnitDetails(new BattleUnitId(1));

        await CaptureAsync(window, "details");

        viewModel.ShowUnitDetails(new BattleUnitId(0));

        await CaptureAsync(window, "details-increase");
    }

    [AvaloniaTest]
    public async Task BattleUnitModifiersTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel) with
        {
            Modifiers =
            [
                UnitModifier.FromEquipment(new UnitModifierDefinitionId("g000um2306")),
                UnitModifier.FromEquipment(new UnitModifierDefinitionId("g000um2402")),
                UnitModifier.FromEquipment(new UnitModifierDefinitionId("g000um2111")),
                new UnitModifier(new UnitModifierDefinitionId("g000um2015"), RemainingTurns: 3),
                UnitModifier.Permanent(new UnitModifierDefinitionId("g000um2007")),
                new UnitModifier(new UnitModifierDefinitionId("g000um9002"), RemainingTurns: 2)
            ],
            WardModifiers = [new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("g000um2401"), new UnitAttackSourceWard(UnitAttackSource.Air, UnitWardCategory.Once))]
        });
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel) with
        {
            Modifiers =
            [
                UnitModifier.FromEquipment(new UnitModifierDefinitionId("g000um2104")),
                new UnitModifier(new UnitModifierDefinitionId("g000um2009"), RemainingTurns: 1)
            ],
            StatusEffects = [new BattlePoisonStatusEffect(RemainingTurns: 2, Damage: 5)]
        });
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel) with
        {
            Modifiers = [UnitModifier.FromEquipment(new UnitModifierDefinitionId("g000um2104"))]
        });

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        await CaptureAsync(window);

        viewModel.ShowUnitDetails(new BattleUnitId(0));

        await CaptureAsync(window, "details");
    }

    [AvaloniaTest]
    public async Task BattleAttackTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        BattleGridTile[] tiles =
        [
            new(BattleSide.Attacker, new GridPosition(0, 0)),
            new(BattleSide.Attacker, new GridPosition(0, 1)),
            new(BattleSide.Attacker, new GridPosition(0, 2)),
            new(BattleSide.Defender, new GridPosition(0, 0)),
            new(BattleSide.Defender, new GridPosition(0, 1)),
            new(BattleSide.Defender, new GridPosition(0, 2))
        ];

        for (int attempt = 0; attempt < 6; attempt++)
        {
            foreach (BattleGridTile tile in tiles)
            {
                Task clickTask = viewModel.ClickAsync(tile);

                if (viewModel.AnimationState is not null)
                {
                    break;
                }
            }

            if (viewModel.AnimationState is not null)
            {
                break;
            }

            Task skipTask = viewModel.SkipCommand.ExecuteAsync(null);
            viewModel.CompleteAnimation(TimeSpan.Zero);
            await skipTask;
        }

        Assert.That(viewModel.AnimationState, Is.Not.Null);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleFullGridsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, new UnitDefinitionId("g000uu0070"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, new UnitDefinitionId("g000uu0071"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, new UnitDefinitionId("g000uu0073"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(3, new UnitDefinitionId("g000uu0052"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(4, new UnitDefinitionId("g000uu0062"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(5, new UnitDefinitionId("g000uu0072"), BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(6, new UnitDefinitionId("g000uu5017"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(7, new UnitDefinitionId("g000uu5113"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(8, new UnitDefinitionId("g000uu5030"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(9, new UnitDefinitionId("g000uu5018"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(10, new UnitDefinitionId("g000uu5034"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(11, new UnitDefinitionId("g000uu5031"), BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(44, 32)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new BattleScreenViewModel(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0)));

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleUltraWideTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, new UnitDefinitionId("g000uu0070"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, new UnitDefinitionId("g000uu0071"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, new UnitDefinitionId("g000uu0073"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(3, new UnitDefinitionId("g000uu0052"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(4, new UnitDefinitionId("g000uu0062"), BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(5, new UnitDefinitionId("g000uu0072"), BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(6, new UnitDefinitionId("g000uu5017"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(7, new UnitDefinitionId("g000uu5113"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(8, new UnitDefinitionId("g000uu5030"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(9, new UnitDefinitionId("g000uu5018"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(10, new UnitDefinitionId("g000uu5034"), BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(11, new UnitDefinitionId("g000uu5031"), BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(44, 32)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, 1920, 810);

        gameViewModel.ShowScreen(new BattleScreenViewModel(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0)));

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleInterleavedRowsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, new UnitDefinitionId("g000uu0057"), BattleSide.Attacker, gameModel), GridPosition.AtFrontLine(1));

        builder.Add(MakeBattleUnit(1, new UnitDefinitionId("g000uu5258"), BattleSide.Defender, gameModel), GridPosition.AtFrontLine(2));
        builder.Add(MakeBattleUnit(2, new UnitDefinitionId("g000uu0087"), BattleSide.Defender, gameModel), GridPosition.AtFrontLine(0));
        builder.Add(MakeBattleUnit(3, new UnitDefinitionId("g000uu5262"), BattleSide.Defender, gameModel), GridPosition.AtBackLine(0));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(19, 3)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        gameViewModel.ShowScreen(new BattleScreenViewModel(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0)));

        await WaitForBattleAsync(window);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleSummonTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);
        UnitDefinitionId summonerId = new("g000uu8012");

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, summonerId, BattleSide.Attacker, gameModel), GridPosition.FrontBottom);

        builder.Add(MakeBattleUnit(1, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(2, defenders.Units[0], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        while (battle.State.CurrentUnitId != new BattleUnitId(0))
        {
            Task skipTask = viewModel.SkipCommand.ExecuteAsync(null);
            viewModel.CompleteAnimation(TimeSpan.Zero);
            await skipTask;
        }

        viewModel.Hover(new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter));

        Assert.That(viewModel.RenderState.Placeholders, Is.Not.Empty);

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleHealTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);
        UnitDefinitionId healerId = new("g000uu0011");

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, healerId, BattleSide.Attacker, gameModel), GridPosition.BackCenter);
        builder.Add(MakeBattleUnit(1, attackers.Leaders[0], BattleSide.Attacker, gameModel) with { Hitpoints = 60 }, GridPosition.FrontCenter);
        builder.Add(MakeBattleUnit(2, attackers.Units[0], BattleSide.Attacker, gameModel) with { Hitpoints = 40 }, GridPosition.FrontTop);

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        while (battle.State.CurrentUnitId != new BattleUnitId(0))
        {
            Task skipTask = viewModel.SkipCommand.ExecuteAsync(null);
            viewModel.CompleteAnimation(TimeSpan.Zero);
            await skipTask;
        }

        viewModel.Hover(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop));

        await CaptureAsync(window);
    }

    [AvaloniaTest]
    public async Task BattleUnitDetailsCursorTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadScenario(null);
        WorldRaceDefinition attackers = gameModel.Definitions.GetRace(WorldRace.Human);
        WorldRaceDefinition defenders = gameModel.Definitions.GetRace(WorldRace.Undead);

        BattleStateBuilder builder = new(gameModel.Definitions);

        builder.Add(MakeBattleUnit(0, attackers.Leaders[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(1, attackers.Units[0], BattleSide.Attacker, gameModel));
        builder.Add(MakeBattleUnit(2, attackers.Units[1], BattleSide.Attacker, gameModel));

        builder.Add(MakeBattleUnit(3, defenders.Leaders[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(4, defenders.Units[0], BattleSide.Defender, gameModel));
        builder.Add(MakeBattleUnit(5, defenders.Units[1], BattleSide.Defender, gameModel));

        EngineContext engine = new(new RandomProvider(0), gameModel.Definitions, gameModel.Stats, GameScenarioProvider.Empty, new GlobalVariables());

        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                engine,
                builder.Build(BattleModifiers.None),
                NullBattleUnitUpgradeResolver.Instance,
                _ => { },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        (GameViewModel gameViewModel, GameView window) = Initialize(sb, BaseWidth, BaseHeight);

        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assets, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.ShowScreen(viewModel);

        await WaitForBattleAsync(window);

        UnitPortrait portrait = window.GetVisualDescendants()
            .OfType<UnitPortrait>()
            .First(control => control.DataContext is BattleGridUnitViewModel unit && unit.UnitId == new BattleUnitId(4));

        Point center = new(portrait.Bounds.Width / 2.0, portrait.Bounds.Height / 2.0);
        Point point = Assert.NotNull(portrait.TranslatePoint(center, window));

        window.MouseMove(point);
        await SkipFramesAsync(5);
        window.MouseDown(point, MouseButton.Right);
        await SkipFramesAsync(5);

        Assert.That(viewModel.InspectedUnit, Is.Not.Null);
        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.CastSpell));

        window.MouseUp(point, MouseButton.Right);
    }
}
