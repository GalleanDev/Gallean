using System.Collections.Immutable;
using System.IO.Compression;
using Gallean.Common;
using Gallean.Configuration;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;
using SkiaSharp;

namespace Gallean.Client.Tests;

public sealed class GameViewModelTests
{
    private sealed class MusicScreen(ScreenMusic initialMusic) : AbstractScreenViewModel
    {
        private ScreenMusic music = initialMusic;

        public override ScreenMusic Music => music;

        public void SetMusic(ScreenMusic value)
        {
            music = value;
            OnPropertyChanged(nameof(Music));
        }
    }

    private sealed class OpenSoundScreen(AssetSoundKey? openSound) : AbstractScreenViewModel
    {
        public override AssetSoundKey? OpenSound => openSound;
    }

    private sealed class CoverScreen : AbstractScreenViewModel
    {
        private bool paintsOwnBackground;

        public override bool PaintsOwnBackground => paintsOwnBackground;

        public void Cover()
        {
            paintsOwnBackground = true;
            OnPropertyChanged(nameof(PaintsOwnBackground));
        }
    }

    private sealed class TrackedScreen(string name, List<string> disposed) : AbstractScreenViewModel
    {
        public override void Dispose()
        {
            disposed.Add(name);
        }

        public override string ToString()
        {
            return name;
        }
    }

    private sealed class TestAudioPlayer : IAudioPlayer
    {
        public List<string> Actions { get; } = [];
        public float SoundVolume { get; set; }
        public float MusicVolume { get; set; }

        public void PlaySound(AssetSoundKey key)
        {
            Actions.Add($"Sound:{key.CanonicalId}");
        }

        public IMusicPlayback? PlayMusic(MusicQueue queue, MusicPosition position)
        {
            Actions.Add($"Music:{string.Join(',', queue.Tracks.Select(track => track.CanonicalId))} from {FormatPosition(position)}");

            return new RecordingPlayback(string.Join(',', queue.Tracks.Select(track => track.CanonicalId)), Actions);
        }

        public IAudioPlayback? PlayVoice(AssetSoundKey key)
        {
            Actions.Add($"Voice:{key.CanonicalId}");

            return new RecordingPlayback(key.CanonicalId, Actions);
        }

        public IAmbientAudioPlayback? PlayAmbientSound(AssetSoundKey key, float volume, float pan)
        {
            return null;
        }
    }

    private sealed class RecordingPlayback(string name, List<string> actions) : IMusicPlayback
    {
        public MusicPosition Position { get; } = new(1, TimeSpan.FromSeconds(30.0));

        public void Stop()
        {
            actions.Add($"Stop:{name}");
        }
    }

    private static List<string> FormatSaves(IReadOnlyList<GameSaveViewModel> saves)
    {
        List<string> rows = [];

        foreach (GameSaveViewModel save in saves)
        {
            rows.Add($"{save.Name} | {save.ScenarioName} | {save.Turn}");
        }

        return rows;
    }

    private static List<string> FormatStacks(WorldState state, WorldPlayerId playerId)
    {
        List<string> rows = [];

        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (stack.OwnerId == playerId)
            {
                foreach ((WorldUnit unit, _) in stack.Units.GetUnits())
                {
                    rows.Add($"{stack.Id} {unit.DefinitionId} level={unit.Level} xp={unit.Experience}");
                }
            }
        }

        return rows;
    }

    private static string FormatPosition(MusicPosition position)
    {
        if (position.Time is null)
        {
            return $"track {position.Track}";
        }
        else
        {
            return $"track {position.Track} at {position.Time}";
        }
    }

    private static ScreenMusic TrackMusic(AssetSoundKey key)
    {
        return ScreenMusic.Tracks(MusicQueue.Create([key], isLooping: true));
    }

    [Test]
    public async Task SaveGameTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioId(new ScenarioId("scenario-1"))
            .BuildSession();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder()
            .WithSaveStore(saveStore)
            .WithClientVersion(new ClientVersion("0.1.0", "2d403009d94320d045b06c4c97671638a00444b2"))
            .Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        gameViewModel.EscapeCommand.Execute(null);

        GameMenuViewModel menu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);

        await menu.SaveGameCommand.ExecuteAsync(null);

        SaveGameScreenViewModel saveScreen = Assert.IsOfType<SaveGameScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(saveScreen.Name, Is.EqualTo("scenario-1:NAME:0, turn 1"));
        Assert.That(saveScreen.SaveLabel, Is.EqualTo("Save"));
        Assert.That(saveScreen.SaveList.Saves, Is.Empty);

        saveScreen.Name = "My save";

        await saveScreen.SaveCommand.ExecuteAsync(null);

        GameSave saved = Assert.NotNull(await saveStore.TryLoadAsync(new GameSaveId(1)));

        Assert.That(
            $"{saved.Metadata.Name} | {saved.Metadata.ScenarioId} | {saved.Metadata.ScenarioName} | {saved.Metadata.TurnNumber} | {saved.Metadata.SavedBy}",
            Is.EqualTo("My save | scenario-1 | scenario-1:NAME:0 | 1 | 0.1.0+2d403009d94320d045b06c4c97671638a00444b2"));
        Assert.That(saved.WorldState, Is.EqualTo(session.World.State));
        Assert.That(saveScreen.Game.Alert.Current, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.SaveGame, "Game saved", "My save")));
    }

    [Test]
    public async Task SaveGameOverwriteTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioId(new ScenarioId("scenario-1"))
            .BuildSession();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Old save", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            session.World.State);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithSaveStore(saveStore).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        gameViewModel.EscapeCommand.Execute(null);

        GameMenuViewModel menu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);

        await menu.SaveGameCommand.ExecuteAsync(null);

        SaveGameScreenViewModel saveScreen = Assert.IsOfType<SaveGameScreenViewModel>(gameViewModel.TopScreen);
        saveScreen.SaveList.SelectedSave = saveScreen.SaveList.Saves[0];

        Assert.That(saveScreen.Name, Is.EqualTo("Old save"));
        Assert.That(saveScreen.SaveLabel, Is.EqualTo("Overwrite"));

        await saveScreen.SaveCommand.ExecuteAsync(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        Assert.That(prompt.Message, Is.EqualTo(TextSpans.FromText("Replace \"Old save\" with the current game?")));

        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(saveScreen.Game.Alert.Current, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.SaveGame, "Game saved", "Old save")));
        Assert.That(FormatSaves(saveScreen.SaveList.Saves), Is.EqualTo(["Old save | scenario-1:NAME:0 | Turn 1"]));
        Assert.That(sb.GetTree(), Is.EquivalentTo(["saves/", "saves/save-1.json.gz"]));
    }

    [Test]
    public async Task SaveGameOverwriteUnreadableTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioId(new ScenarioId("scenario-1"))
            .BuildSession();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        sb.CreateDirectory("saves");
        sb.WriteTextFile(Path.Combine("saves", "save-1.json.gz"), "damaged");

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithSaveStore(saveStore).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        gameViewModel.EscapeCommand.Execute(null);

        GameMenuViewModel menu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);

        await menu.SaveGameCommand.ExecuteAsync(null);

        SaveGameScreenViewModel saveScreen = Assert.IsOfType<SaveGameScreenViewModel>(gameViewModel.TopScreen);
        saveScreen.SaveList.SelectedSave = saveScreen.SaveList.Saves[0];

        Assert.That(saveScreen.Name, Is.EqualTo("scenario-1:NAME:0, turn 1"));
        Assert.That(saveScreen.SaveLabel, Is.EqualTo("Overwrite"));

        await saveScreen.SaveCommand.ExecuteAsync(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        Assert.That(prompt.Message, Is.EqualTo(TextSpans.FromText("Replace \"Unreadable save\" with the current game?")));

        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(FormatSaves(saveScreen.SaveList.Saves), Is.EqualTo(["scenario-1:NAME:0, turn 1 | scenario-1:NAME:0 | Turn 1"]));
        Assert.That(sb.GetTree(), Is.EquivalentTo(["saves/", "saves/save-1.json.gz"]));
    }

    [Test]
    public async Task LoadGameTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        (GameSession session, _) = gameLoader.LoadScenario("S137SC0000", worldState: null);
        WorldState savedState = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(10, new WorldPlayerId(2))
        };
        GameSaveMetadata metadata = new("Saved game", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 10, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(new GameSaveId(1), metadata, savedState);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithGameLoader(gameLoader).WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        LoadGameScreenViewModel loadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatSaves(loadScreen.SaveList.Saves), Is.EqualTo(["Saved game | Conquest of Thelena | Turn 10"]));

        await gameViewModel.LoadGameCommand.ExecuteAsync(loadScreen.SaveList.SelectedSave?.Entry);

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        world.CompleteGameStart();

        Assert.That(world.GameModel.ScenarioId, Is.EqualTo("S137SC0000"));
        Assert.That(world.GameModel.Snapshot, Is.EqualTo(
            new GameSnapshot(savedState, BattleState: null, ActiveActions: 0, ViewPlayerId: new WorldPlayerId(2))));
        Assert.That(
            await saveStore.EnumerateAsync(),
            Is.EqualTo([new GameSaveEntry(new GameSaveId(1), Result<GameSaveMetadata>.FromValue(metadata))]));
    }

    [Test]
    public async Task LoadWonGameTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        (GameSession session, _) = gameLoader.LoadScenario("S137SC0000", worldState: null);
        WorldState savedState = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(9, new WorldPlayerId(2)),
            ScenarioState = session.World.State.ScenarioState with { IsEnded = true, Winner = new WorldPlayerId(2) }
        };
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Victory", GameSaveType.Victory, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            savedState);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithGameLoader(gameLoader).WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.LoadGameCommand.ExecuteAsync(new GameSaveEntry(
            new GameSaveId(1),
            Result<GameSaveMetadata>.FromValue(new GameSaveMetadata("Victory", GameSaveType.Victory, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))));

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        world.CompleteGameStart();

        GameEndedScreenViewModel gameEnded = Assert.IsOfType<GameEndedScreenViewModel>(gameViewModel.TopScreen);
        GameSave saved = Assert.NotNull(await saveStore.TryLoadAsync(new GameSaveId(1)));

        Assert.That(gameEnded.IsVictory, Is.True);
        Assert.That(gameEnded.CanContinueCampaign, Is.True);
        Assert.That(world.GameModel.Snapshot, Is.EqualTo(
            new GameSnapshot(savedState, BattleState: null, ActiveActions: 0, ViewPlayerId: new WorldPlayerId(2))));
        Assert.That(saved.Metadata.SavedAt, Is.EqualTo(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)));
        Assert.That(sb.GetTree(), Is.EquivalentTo(["saves/", "saves/save-1.json.gz"]));
    }

    [Test]
    public async Task ContinueCampaignWithoutLeadersTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        (GameSession session, _) = gameLoader.LoadScenario("S137SC0000", worldState: null);
        ImmutableSortedDictionary<WorldStackId, WorldStack>.Builder stacks = session.World.State.Stacks.ToBuilder();

        foreach (WorldStack stack in session.World.State.Stacks.Values)
        {
            if (stack.OwnerId == new WorldPlayerId(2))
            {
                stacks[stack.Id] = stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(new WorldUnit(new WorldUnitId("unit"), new UnitDefinitionId("G000UU0024"), 175, 0, 1, [], null, false), GridPosition.FrontCenter, true)
                        .Build()
                };
            }
        }

        WorldState savedState = session.World.State with
        {
            Stacks = stacks.ToImmutable(),
            TurnInfo = new WorldTurnInfo(9, new WorldPlayerId(2)),
            ScenarioState = session.World.State.ScenarioState with { IsEnded = true, Winner = new WorldPlayerId(2) }
        };
        GameSaveMetadata metadata = new("Victory", GameSaveType.Victory, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(new GameSaveId(1), metadata, savedState);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithGameLoader(gameLoader).WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.LoadGameCommand.ExecuteAsync(new GameSaveEntry(new GameSaveId(1), Result<GameSaveMetadata>.FromValue(metadata)));

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        world.CompleteGameStart();

        GameEndedScreenViewModel gameEnded = Assert.IsOfType<GameEndedScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(gameEnded.CanContinueCampaign, Is.True);

        await gameViewModel.ContinueCampaignCommand.ExecuteAsync(null);

        BriefingScreenViewModel briefing = Assert.IsOfType<BriefingScreenViewModel>(gameViewModel.TopScreen);

        await briefing.ContinueCommand.ExecuteAsync(null);

        WorldScreenViewModel nextWorld = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        GameSnapshot snapshot = nextWorld.GameModel.Snapshot;

        Assert.That(nextWorld.GameModel.ScenarioId, Is.EqualTo("S138SC0000"));
        Assert.That(snapshot.ViewPlayerId, Is.EqualTo(new WorldPlayerId(4)));
        Assert.That(FormatStacks(snapshot.WorldState, snapshot.ViewPlayerId), Is.EqualTo([
            "S138KC004e G000UU8009 level=1 xp=0"
        ]));
    }

    [Test]
    public async Task LoadConfiguredGameTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Elf, LordCategory.Mage, new WorldObjectVariant(2))], WorldDifficulty.Hard, Leader: null);
        (GameSession session, _) = gameLoader.CreateScenario("S137SC0000", configuration);
        WorldState savedState = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(9, new WorldPlayerId(2))
        };
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Saved game", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            savedState);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithGameLoader(gameLoader).WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.LoadGameCommand.ExecuteAsync(new GameSaveEntry(
            new GameSaveId(1),
            Result<GameSaveMetadata>.FromValue(new GameSaveMetadata("Saved game", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))));

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        GameSnapshot snapshot = world.GameModel.Snapshot;
        WorldPlayer player = snapshot.WorldState.Players[new WorldPlayerId(2)];

        Assert.That(snapshot, Is.EqualTo(new GameSnapshot(savedState, BattleState: null, ActiveActions: 0, ViewPlayerId: new WorldPlayerId(2))));
        Assert.That(snapshot.WorldState.Difficulty, Is.EqualTo(WorldDifficulty.Hard));
        Assert.That(player.LordId, Is.EqualTo(new LordDefinitionId("G000LR0018")));
        Assert.That(player.FaceVariant, Is.EqualTo(new WorldObjectVariant(2)));
        Assert.That(player.Resources, Is.EqualTo(new WorldResourceSet(275, 0, 0, 0, 0, 12)));
    }

    [Test]
    public async Task LoadGameOnAiTurnTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        (GameSession session, _) = gameLoader.LoadScenario("S137SC0000", worldState: null);
        WorldState savedState = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(9, new WorldPlayerId(1))
        };
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Saved game", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            savedState);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithGameLoader(gameLoader).WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.LoadGameCommand.ExecuteAsync(new GameSaveEntry(
            new GameSaveId(1),
            Result<GameSaveMetadata>.FromValue(new GameSaveMetadata("Saved game", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))));

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);

        Assert.That(world.GameModel.Snapshot.WorldState.TurnInfo, Is.EqualTo(new WorldTurnInfo(10, new WorldPlayerId(2))));
    }

    [Test]
    public async Task LoadGamePromptsTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        (GameSession session, _) = gameLoader.LoadScenario("S137SC0000", worldState: null);
        WorldState initialState = session.World.State;
        WorldStack stack = initialState.Stacks[new WorldStackId("S137KC0000")];
        WorldPlayer player = initialState.Players[new WorldPlayerId(2)];
        WorldDiplomacyProposal proposal = new(new WorldPlayerId(1), new WorldPlayerId(2), new WorldDiplomacyProposeAllianceAction());
        WorldState savedState = initialState with
        {
            TurnInfo = new WorldTurnInfo(9, new WorldPlayerId(2)),
            Stacks = initialState.Stacks.SetItem(stack.Id, stack with { PendingLeaderUpgrades = 1 }),
            Players = initialState.Players.SetItem(player.Id, player with { IncomingDiplomacyProposals = [proposal] })
        };
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Saved game", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            savedState);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithGameLoader(gameLoader).WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.LoadGameCommand.ExecuteAsync(new GameSaveEntry(
            new GameSaveId(1),
            Result<GameSaveMetadata>.FromValue(new GameSaveMetadata("Saved game", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))));

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        world.CompleteGameStart();

        LeaderUpgradeViewModel upgradeScreen = Assert.IsOfType<LeaderUpgradeViewModel>(gameViewModel.TopScreen);

        upgradeScreen.Close();

        DiplomacyProposalViewModel proposalScreen = Assert.IsOfType<DiplomacyProposalViewModel>(gameViewModel.TopScreen);

        await proposalScreen.DeclineCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
    }

    [Test]
    public async Task LoadGameFromGameMenuTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        (GameSession session, _) = gameLoader.LoadScenario("S137SC0000", worldState: null);
        WorldState firstState = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(9, new WorldPlayerId(2))
        };
        WorldState secondState = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(12, new WorldPlayerId(2))
        };
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("First save", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            firstState);
        await saveStore.SaveAsync(
            new GameSaveId(2),
            new GameSaveMetadata("Second save", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 12, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            secondState);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithGameLoader(gameLoader).WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.LoadGameCommand.ExecuteAsync(new GameSaveEntry(
            new GameSaveId(1),
            Result<GameSaveMetadata>.FromValue(new GameSaveMetadata("First save", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))));

        WorldScreenViewModel firstWorld = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        firstWorld.CompleteGameStart();

        gameViewModel.EscapeCommand.Execute(null);

        GameMenuViewModel menu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);

        await menu.Game.ShowLoadGameCommand.ExecuteAsync(null);

        LoadGameScreenViewModel loadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(
            FormatSaves(loadScreen.SaveList.Saves),
            Is.EqualTo(["Second save | Conquest of Thelena | Turn 12", "First save | Conquest of Thelena | Turn 9"]));

        await gameViewModel.LoadGameCommand.ExecuteAsync(loadScreen.SaveList.SelectedSave?.Entry);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { firstWorld, menu, loadScreen, prompt }));
            Assert.That(prompt.Title, Is.EqualTo("Load game"));
            Assert.That(prompt.Message, Is.EqualTo(TextSpans.FromText("Do you want to load \"Second save\"? Unsaved progress will be lost.")));
            Assert.That(prompt.ConfirmLabel, Is.EqualTo("Load"));
        }

        await prompt.ConfirmCommand.ExecuteAsync(null);

        WorldScreenViewModel secondWorld = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        secondWorld.CompleteGameStart();

        Assert.That(gameViewModel.Screens, Is.EqualTo([secondWorld]));
        Assert.That(secondWorld.GameModel.Snapshot, Is.EqualTo(
            new GameSnapshot(secondState, BattleState: null, ActiveActions: 0, ViewPlayerId: new WorldPlayerId(2))));
    }

    [Test]
    public async Task ExitAfterSaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        (GameSession session, _) = gameLoader.LoadScenario("S137SC0000", worldState: null);
        WorldState state = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(9, new WorldPlayerId(2))
        };
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("First save", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            state);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithGameLoader(gameLoader).WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.LoadGameCommand.ExecuteAsync(new GameSaveEntry(
            new GameSaveId(1),
            Result<GameSaveMetadata>.FromValue(new GameSaveMetadata("First save", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))));

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        world.CompleteGameStart();

        gameViewModel.EscapeCommand.Execute(null);

        GameMenuViewModel menu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);

        await menu.SaveGameCommand.ExecuteAsync(null);

        SaveGameScreenViewModel saveScreen = Assert.IsOfType<SaveGameScreenViewModel>(gameViewModel.TopScreen);
        saveScreen.Name = "Second save";

        await saveScreen.SaveCommand.ExecuteAsync(null);

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { world, menu }));

        await menu.Game.ExitGameCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([gameViewModel.RootScreen]));
        Assert.That(gameViewModel.RootScreen, Is.InstanceOf<MainMenuScreenViewModel>());
    }

    [Test]
    public async Task ExitSaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        (GameSession session, _) = gameLoader.LoadScenario("S137SC0000", worldState: null);
        WorldState state = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(9, new WorldPlayerId(2))
        };
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("First save", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            state);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithGameLoader(gameLoader).WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.LoadGameCommand.ExecuteAsync(new GameSaveEntry(
            new GameSaveId(1),
            Result<GameSaveMetadata>.FromValue(new GameSaveMetadata("First save", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))));

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        world.CompleteGameStart();

        gameViewModel.EscapeCommand.Execute(null);

        GameMenuViewModel gameMenu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);
        await gameMenu.Game.ExitGameCommand.ExecuteAsync(null);

        MainMenuScreenViewModel menu = Assert.IsOfType<MainMenuScreenViewModel>(gameViewModel.RootScreen);
        GameSaveViewModel latestSave = Assert.NotNull(menu.LatestSave);
        GameSave saved = Assert.NotNull(await saveStore.TryLoadAsync(latestSave.Id));

        Assert.That(gameViewModel.Screens, Is.EqualTo([menu]));
        Assert.That(latestSave.Id, Is.EqualTo(new GameSaveId(2)));
        Assert.That(
            $"{saved.Metadata.Name} | {saved.Metadata.Type} | {saved.Metadata.ScenarioId} | {saved.Metadata.ScenarioName} | {saved.Metadata.TurnNumber}",
            Is.EqualTo("Exit save | Exit | S137SC0000 | Conquest of Thelena | 9"));
        Assert.That(saved.WorldState, Is.EqualTo(state));

        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        LoadGameScreenViewModel loadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(
            FormatSaves(loadScreen.SaveList.Saves),
            Is.EqualTo(["Exit save | Conquest of Thelena | Turn 9", "First save | Conquest of Thelena | Turn 9"]));
    }

    [Test]
    public async Task ContinueTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        (GameSession session, _) = gameLoader.LoadScenario("S137SC0000", worldState: null);
        WorldState olderState = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(9, new WorldPlayerId(2))
        };
        WorldState newerState = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(12, new WorldPlayerId(2))
        };
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Newer save", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 12, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            newerState);
        await saveStore.SaveAsync(
            new GameSaveId(2),
            new GameSaveMetadata("Older save", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            olderState);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithGameLoader(gameLoader).WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        MainMenuScreenViewModel menu = Assert.IsOfType<MainMenuScreenViewModel>(gameViewModel.RootScreen);

        GameSaveViewModel latestSave = Assert.NotNull(menu.LatestSave);

        Assert.That(latestSave.Entry, Is.EqualTo(new GameSaveEntry(
            new GameSaveId(1),
            Result<GameSaveMetadata>.FromValue(new GameSaveMetadata("Newer save", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 12, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300")))));

        await gameViewModel.LoadGameCommand.ExecuteAsync(latestSave.Entry);

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);

        Assert.That(world.GameModel.Snapshot, Is.EqualTo(
            new GameSnapshot(newerState, BattleState: null, ActiveActions: 0, ViewPlayerId: new WorldPlayerId(2))));
    }

    [Test]
    public async Task ContinueAfterVictoryTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        (GameSession session, _) = gameLoader.LoadScenario("S137SC0000", worldState: null);
        WorldState olderState = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(9, new WorldPlayerId(2))
        };
        WorldState victoryState = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(12, new WorldPlayerId(2)),
            ScenarioState = session.World.State.ScenarioState with { IsEnded = true, Winner = new WorldPlayerId(2) }
        };
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Victory", GameSaveType.Victory, "S137SC0000", "Conquest of Thelena", 12, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            victoryState);
        await saveStore.SaveAsync(
            new GameSaveId(2),
            new GameSaveMetadata("Older save", GameSaveType.Manual, "S137SC0000", "Conquest of Thelena", 9, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            olderState);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithGameLoader(gameLoader).WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        MainMenuScreenViewModel menu = Assert.IsOfType<MainMenuScreenViewModel>(gameViewModel.RootScreen);

        GameSaveViewModel latestSave = Assert.NotNull(menu.LatestSave);

        Assert.That(latestSave.Entry, Is.EqualTo(new GameSaveEntry(
            new GameSaveId(1),
            Result<GameSaveMetadata>.FromValue(new GameSaveMetadata("Victory", GameSaveType.Victory, "S137SC0000", "Conquest of Thelena", 12, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300")))));

        await gameViewModel.LoadGameCommand.ExecuteAsync(latestSave.Entry);

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);

        Assert.That(world.GameModel.Snapshot, Is.EqualTo(
            new GameSnapshot(victoryState, BattleState: null, ActiveActions: 0, ViewPlayerId: new WorldPlayerId(2))));
    }

    [Test]
    public async Task ContinueWithoutSaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithSaveStore(new GameSaveStore(sb.GetPath("saves"), 10)).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        MainMenuScreenViewModel menu = Assert.IsOfType<MainMenuScreenViewModel>(gameViewModel.RootScreen);

        Assert.That(menu.LatestSave, Is.Null);
        Assert.That(gameViewModel.LoadGameCommand.CanExecute(menu.LatestSave?.Entry), Is.False);
    }

    [Test]
    public async Task StartGameTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithSaveStore(new GameSaveStore(sb.GetPath("saves"), 10)).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.StartGameCommand.ExecuteAsync(new GameConfiguration(GameLoader.TutorialScenarioId, Configuration: null));

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<LoadingScreenViewModel>());
        Assert.That(world.GameModel.ScenarioId, Is.EqualTo(GameLoader.TutorialScenarioId));
        Assert.That(world.GameModel.Snapshot.WorldState.TurnInfo, Is.EqualTo(new WorldTurnInfo(1, new WorldPlayerId(2))));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<LoadingScreenViewModel>());
    }

    [Test]
    public async Task LoadGameWithoutSaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithSaveStore(new GameSaveStore(sb.GetPath("saves"), 10)).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.RootScreen, Is.InstanceOf<MainMenuScreenViewModel>());

        LoadGameScreenViewModel loadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(loadScreen.SaveList.Saves, Is.Empty);
        Assert.That(loadScreen.DeleteCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task DeleteSaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Doomed", GameSaveType.Manual, "scenario-1", "Scenario One", 3, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            session.World.State);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        LoadGameScreenViewModel loadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatSaves(loadScreen.SaveList.Saves), Is.EqualTo(["Doomed | Scenario One | Turn 3"]));

        loadScreen.DeleteCommand.Execute(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        Assert.That(prompt.Message, Is.EqualTo(TextSpans.FromText("Delete \"Doomed\" permanently?")));

        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(loadScreen.SaveList.Saves, Is.Empty);
        Assert.That(await saveStore.EnumerateAsync(), Is.Empty);
    }

    [Test]
    public async Task DeleteUnreadableSaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Kept", GameSaveType.Manual, "scenario-1", "Scenario One", 3, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            session.World.State);
        sb.WriteTextFile(Path.Combine("saves", "save-2.json.gz"), "damaged");

        await using (FileStream stream = File.Create(sb.GetPath(Path.Combine("saves", "save-3.json.gz"))))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write("""{"formatVersion":1,"scenarioId":"scenario-1"}"""u8);
        }

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        LoadGameScreenViewModel loadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(
            FormatSaves(loadScreen.SaveList.Saves),
            Is.EqualTo([
                "Kept | Scenario One | Turn 3",
                "Unreadable save | Incompatible game save | ",
                "Unreadable save | Invalid game save | "
            ]));

        loadScreen.SaveList.SelectedSave = loadScreen.SaveList.Saves[1];

        Assert.That(gameViewModel.LoadGameCommand.CanExecute(loadScreen.SaveList.SelectedSave.Entry), Is.False);

        loadScreen.DeleteCommand.Execute(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        Assert.That(prompt.Message, Is.EqualTo(TextSpans.FromText("Delete \"Unreadable save\" permanently?")));

        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(
            FormatSaves(loadScreen.SaveList.Saves),
            Is.EqualTo(["Kept | Scenario One | Turn 3", "Unreadable save | Invalid game save | "]));
        Assert.That(sb.GetTree(), Is.EquivalentTo(["saves/", "saves/save-1.json.gz", "saves/save-2.json.gz"]));
    }

    [Test]
    public void MusicLayersTest()
    {
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        MusicScreen current = new(TrackMusic(new AssetSoundKey.MenuMusic()));
        MusicScreen overlay = new(ScreenMusic.Keep);
        MusicScreen modal = new(ScreenMusic.Keep);

        gameViewModel.ShowScreen(current);
        gameViewModel.OpenScreen(overlay);
        gameViewModel.OpenScreen(modal);
        modal.SetMusic(TrackMusic(new AssetSoundKey.BattleMusic(Variant: 0)));
        modal.SetMusic(ScreenMusic.Keep);

        Assert.That(
            audioPlayer.Actions,
            Is.EqualTo(
            [
                "Music:menu-music from track 0",
                "Stop:menu-music",
                "Music:battle-music/0 from track 0",
                "Stop:battle-music/0",
                "Music:menu-music from track 1 at 00:00:30"
            ]));
    }

    [Test]
    public void MusicOverlayTest()
    {
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        MusicScreen current = new(TrackMusic(new AssetSoundKey.WorldMusic(Variant: 0)));
        MusicScreen overlay = new(ScreenMusic.None);

        gameViewModel.ShowScreen(current);
        gameViewModel.OpenScreen(overlay);
        gameViewModel.CloseScreen(overlay);

        Assert.That(
            audioPlayer.Actions,
            Is.EqualTo(
            [
                "Music:world-music/0 from track 0",
                "Stop:world-music/0",
                "Music:world-music/0 from track 1 at 00:00:30"
            ]));
    }

    [Test]
    public void MusicScreenAboveTest()
    {
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        MusicScreen current = new(TrackMusic(new AssetSoundKey.WorldMusic(Variant: 0)));
        MusicScreen above = new(TrackMusic(new AssetSoundKey.CapitalMusic(WorldRace.Human, Variant: 0)));

        gameViewModel.ShowScreen(current);
        gameViewModel.OpenScreen(above);
        gameViewModel.CloseScreen(above);

        Assert.That(
            audioPlayer.Actions,
            Is.EqualTo(
            [
                "Music:world-music/0 from track 0",
                "Stop:world-music/0",
                "Music:capital-music/human/0 from track 0",
                "Stop:capital-music/human/0",
                "Music:world-music/0 from track 1 at 00:00:30"
            ]));
    }

    [Test]
    public void MusicNewRootTest()
    {
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        MusicScreen menu = new(TrackMusic(new AssetSoundKey.MenuMusic()));
        MusicScreen world = new(TrackMusic(new AssetSoundKey.WorldMusic(Variant: 0)));
        MusicScreen returnedMenu = new(TrackMusic(new AssetSoundKey.MenuMusic()));

        gameViewModel.ShowScreen(menu);
        gameViewModel.ShowScreen(world);
        gameViewModel.ShowScreen(returnedMenu);

        Assert.That(
            audioPlayer.Actions,
            Is.EqualTo(
            [
                "Music:menu-music from track 0",
                "Stop:menu-music",
                "Music:world-music/0 from track 0",
                "Stop:world-music/0",
                "Music:menu-music from track 0"
            ]));
    }

    [Test]
    public async Task GameEndedVictoryTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^.."
        ]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            human.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            ai.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(stack)
            .WithCapital(capital)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithSaveStore(saveStore).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(stack.Id, capital.Entrance));

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        world.CompleteAnimation();

        GameEndedScreenViewModel gameEnded = Assert.IsOfType<GameEndedScreenViewModel>(gameViewModel.TopScreen);
        Assert.That(gameEnded.IsVictory, Is.True);
        Assert.That(gameEnded.Title, Is.EqualTo("Victory"));
        Assert.That(gameEnded.ScenarioName, Is.EqualTo("test-scenario:NAME:0"));
        Assert.That(gameEnded.Debriefing, Is.EqualTo([new ScenarioTextSpan("test-scenario:DEBUNKW:0", ScenarioTextSpanType.Text)]));
        Assert.That(gameEnded.Music.Queue?.Tracks, Is.EqualTo([new AssetSoundKey.VictoryMusic(new ScenarioId("test-scenario"))]));
        Assert.That(gameEnded.Music.Queue?.IsLooping, Is.False);
        Assert.That(gameEnded.CanContinueCampaign, Is.False);

        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        LoadGameScreenViewModel loadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);
        GameSave saved = Assert.NotNull(await saveStore.TryLoadAsync(new GameSaveId(1)));

        Assert.That(FormatSaves(loadScreen.SaveList.Saves), Is.EqualTo(["Victory | test-scenario:NAME:0 | Turn 1"]));
        Assert.That(
            $"{saved.Metadata.Name} | {saved.Metadata.Type} | {saved.Metadata.ScenarioId} | {saved.Metadata.ScenarioName} | {saved.Metadata.TurnNumber}",
            Is.EqualTo("Victory | Victory | test-scenario | test-scenario:NAME:0 | 1"));
        Assert.That(saved.WorldState, Is.EqualTo(session.World.State));
    }

    [Test]
    public async Task GameEndedDefeatTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };
        WorldCapital capturedCapital = TestHelpers.SimpleCapital(
                new WorldFortificationId("capital"),
                human.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                CapturedById = ai.Id
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithCapital(capturedCapital)
            .WithScenarioState(WorldScenarioState.Empty with { IsEnded = true, Winner = ai.Id })
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        GameEndedScreenViewModel gameEnded = Assert.IsOfType<GameEndedScreenViewModel>(gameViewModel.TopScreen);
        Assert.That(gameEnded.IsVictory, Is.False);
        Assert.That(gameEnded.Title, Is.EqualTo("Defeat"));
        Assert.That(gameEnded.Debriefing, Is.EqualTo([new ScenarioTextSpan("test-scenario:DEBUNKL:0", ScenarioTextSpanType.Text)]));
        Assert.That(gameEnded.Music.Queue?.Tracks, Is.EqualTo([new AssetSoundKey.BriefingMusic()]));
        Assert.That(gameEnded.Music.Queue?.IsLooping, Is.False);
    }

    [Test]
    public async Task GameEndedScenarioLostTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventWinEffect(WinnerId: null)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithSaveStore(saveStore).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        GameEndedScreenViewModel gameEnded = Assert.IsOfType<GameEndedScreenViewModel>(gameViewModel.TopScreen);
        Assert.That(gameEnded.IsVictory, Is.False);
        Assert.That(gameEnded.Title, Is.EqualTo("Defeat"));
        Assert.That(gameEnded.Debriefing, Is.EqualTo([new ScenarioTextSpan("test-scenario:DEBUNKL:0", ScenarioTextSpanType.Text)]));
        Assert.That(await saveStore.EnumerateAsync(), Is.Empty);
    }

    [Test]
    public async Task GameEndedScenarioLostPopupTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("Bethrezen is dead!"),
                    ScenarioEventPopupAudience.AllPlayers,
                    null,
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventWinEffect(WinnerId: null)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ScenarioEventPopupViewModel popup = Assert.IsOfType<ScenarioEventPopupViewModel>(gameViewModel.TopScreen);
        Assert.That(popup.Messages, Is.EqualTo([
            new ScenarioEventPopupMessageViewModel(new ScenarioEventPopupMessage([new ScenarioTextSpan("Bethrezen is dead!", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true))
        ]));

        popup.CloseCommand.Execute(null);

        GameEndedScreenViewModel gameEnded = Assert.IsOfType<GameEndedScreenViewModel>(gameViewModel.TopScreen);
        Assert.That(gameEnded.IsVictory, Is.False);
        Assert.That(gameEnded.Title, Is.EqualTo("Defeat"));
        Assert.That(gameEnded.Debriefing, Is.EqualTo([new ScenarioTextSpan("test-scenario:DEBUNKL:0", ScenarioTextSpanType.Text)]));
    }

    [Test]
    public async Task AiTurnAfterDefeatTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldCapital capturedCapital = TestHelpers.SimpleCapital(
                new WorldFortificationId("capital"),
                human.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                CapturedById = ai.Id
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithCapital(capturedCapital)
            .WithScenarioState(WorldScenarioState.Empty with { IsEnded = true, Winner = ai.Id })
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        GameSnapshot initialSnapshot = gameModel.Snapshot;

        await gameViewModel.ExecuteAiTurnsAsync(world);

        Assert.That(gameModel.Snapshot, Is.EqualTo(initialSnapshot));
    }

    [Test]
    public async Task AiTurnAfterHotseatDefeatTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer defeated = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Heretic, map);
        WorldCapital capturedCapital = TestHelpers.SimpleCapital(
                new WorldFortificationId("capital"),
                defeated.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                CapturedById = ai.Id
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, ai.Id))
            .WithDefinition(TestHelpers.SimpleLord(defeated.LordId, defeated.Race, LordCategory.Warrior))
            .WithPlayer(defeated)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithCapital(capturedCapital)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        WorldState expectedWorld = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(2, human.Id),
            Players = session.World.State.Players.SetItem(
                human.Id,
                human with { Resources = new WorldResourceSet(100, 0, 0, 0, 25, 0) }),
            AiMemories = session.World.State.AiMemories.SetItem(
                ai.Id,
                new WorldAiMemory(new WorldAiTurnMemory(1, ai.Id)))
        };
        GameSnapshot expectedSnapshot = new(expectedWorld, BattleState: null, ActiveActions: 0, ViewPlayerId: human.Id);

        await gameViewModel.ExecuteAiTurnsAsync(world);

        Assert.That(gameModel.Snapshot, Is.EqualTo(expectedSnapshot));
        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
    }

    [Test]
    public async Task ExecuteAiTurnsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        WorldState expectedWorld = session.World.State with
        {
            TurnInfo = new WorldTurnInfo(2, human.Id),
            Players = session.World.State.Players.SetItem(
                human.Id,
                human with { Resources = new WorldResourceSet(100, 0, 0, 0, 25, 0) }),
            AiMemories = session.World.State.AiMemories.SetItem(
                ai.Id,
                new WorldAiMemory(new WorldAiTurnMemory(session.World.State.TurnInfo.TurnNumber, ai.Id)))
        };
        GameSnapshot expectedSnapshot = new(expectedWorld, BattleState: null, ActiveActions: 0, ViewPlayerId: human.Id);

        await gameViewModel.ExecuteAiTurnsAsync(world);

        Assert.That(gameModel.Snapshot, Is.EqualTo(expectedSnapshot));
    }

    [Test]
    public void ScreenStackTest()
    {
        List<string> disposed = [];
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        TrackedScreen root = new("root", disposed);
        TrackedScreen management = new("management", disposed);
        TrackedScreen prompt = new("prompt", disposed);

        gameViewModel.ShowScreen(root);
        gameViewModel.OpenScreen(management);
        gameViewModel.OpenScreen(prompt);

        Assert.That(gameViewModel.Screens.Select(screen => screen.ToString()), Is.EqualTo(["root", "management", "prompt"]));
        Assert.That(gameViewModel.RootScreen, Is.SameAs(root));
        Assert.That(gameViewModel.TopScreen, Is.SameAs(prompt));
        Assert.That(disposed, Is.Empty);

        gameViewModel.CloseScreen(prompt);

        Assert.That(gameViewModel.Screens.Select(screen => screen.ToString()), Is.EqualTo(["root", "management"]));
        Assert.That(disposed, Is.EqualTo(["prompt"]));
    }

    [Test]
    public void CloseScreenBelowTopTest()
    {
        List<string> disposed = [];
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        TrackedScreen root = new("root", disposed);
        TrackedScreen management = new("management", disposed);
        TrackedScreen prompt = new("prompt", disposed);

        gameViewModel.ShowScreen(root);
        gameViewModel.OpenScreen(management);
        gameViewModel.OpenScreen(prompt);
        gameViewModel.CloseScreen(management);

        Assert.That(gameViewModel.Screens.Select(screen => screen.ToString()), Is.EqualTo(["root"]));
        Assert.That(disposed, Is.EqualTo(["prompt", "management"]));

        gameViewModel.CloseScreen(management);

        Assert.That(disposed, Is.EqualTo(["prompt", "management"]));
    }

    [Test]
    public void ShowScreenTest()
    {
        List<string> disposed = [];
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        TrackedScreen root = new("root", disposed);
        TrackedScreen management = new("management", disposed);
        TrackedScreen mainMenu = new("main-menu", disposed);

        gameViewModel.ShowScreen(root);
        gameViewModel.OpenScreen(management);
        gameViewModel.ShowScreen(mainMenu);

        Assert.That(gameViewModel.Screens.Select(screen => screen.ToString()), Is.EqualTo(["main-menu"]));
        Assert.That(disposed, Is.EqualTo(["management", "root"]));
    }

    [Test]
    public void ShowScreenSameRootTest()
    {
        List<string> disposed = [];
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        TrackedScreen root = new("root", disposed);
        TrackedScreen management = new("management", disposed);

        gameViewModel.ShowScreen(root);
        gameViewModel.OpenScreen(management);
        gameViewModel.ShowScreen(root);

        Assert.That(gameViewModel.Screens.Select(screen => screen.ToString()), Is.EqualTo(["root"]));
        Assert.That(disposed, Is.EqualTo(["management"]));
    }

    [Test]
    public void DisposeTest()
    {
        List<string> disposed = [];
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        TrackedScreen root = new("root", disposed);
        TrackedScreen management = new("management", disposed);

        gameViewModel.ShowScreen(root);
        gameViewModel.OpenScreen(management);
        gameViewModel.Dispose();

        Assert.That(gameViewModel.Screens.Select(screen => screen.ToString()), Is.Empty);
        Assert.That(disposed, Is.EqualTo(["management", "root"]));
    }

    [Test]
    public void CoverTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        CoverScreen screen = new();
        List<string?> changes = [];

        gameViewModel.ShowScreen(screen);
        gameViewModel.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        Assert.That(gameViewModel.PaintsOwnBackground, Is.False);

        screen.Cover();

        Assert.That(gameViewModel.PaintsOwnBackground, Is.True);
        Assert.That(changes, Is.EqualTo([nameof(GameViewModel.RootScreen), nameof(GameViewModel.TopScreen), nameof(GameViewModel.PaintsOwnBackground)]));
    }

    [Test]
    public void CloseUnopenedScreenTest()
    {
        List<string> disposed = [];
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        TrackedScreen root = new("root", disposed);
        TrackedScreen orphan = new("orphan", disposed);

        gameViewModel.ShowScreen(root);
        gameViewModel.CloseScreen(orphan);

        Assert.That(gameViewModel.Screens.Select(screen => screen.ToString()), Is.EqualTo(["root"]));
        Assert.That(disposed, Is.EqualTo(["orphan"]));
    }

    [Test]
    public async Task AiTurnBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 60, damage: 5) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("AiStack"),
            ai.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("HumanStack"),
            human.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(aiStack)
            .WithStack(humanStack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Task task = gameViewModel.ExecuteAiTurnsAsync(world);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<BattleScreenViewModel>());
        Assert.That(task.IsCompleted, Is.False);

        BattleScreenViewModel battleScreen = Assert.IsOfType<BattleScreenViewModel>(gameViewModel.TopScreen);

        await battleScreen.CompleteTransitionAsync();
        battleScreen.AutoResolveCommand.Execute(null);

        ConfirmPromptViewModel autoResolvePrompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await autoResolvePrompt.ConfirmCommand.ExecuteAsync(null);

        battleScreen.ExitBattleCommand.Execute(null);
        await battleScreen.CompleteTransitionAsync();

        Assert.That(task.IsCompleted, Is.True);
        Assert.That(gameModel.Snapshot.BattleState, Is.Null);
        Assert.That(gameModel.Snapshot.WorldState.TurnInfo.CurrentPlayerId, Is.EqualTo(human.Id));
    }

    [Test]
    public async Task AiTurnBattleLoadFailureTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 60, damage: 5) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("AiStack"),
            ai.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("HumanStack"),
            human.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(aiStack)
            .WithStack(humanStack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Task task = gameViewModel.ExecuteAiTurnsAsync(world);

        Assert.That(task.IsCompleted, Is.True);
        ErrorScreenViewModel error = Assert.IsOfType<ErrorScreenViewModel>(gameViewModel.TopScreen);
        Assert.That(error.Message, Does.StartWith("Failed to load battle."));
        Assert.That(gameModel.Snapshot.BattleState, Is.Null);
        Assert.That(gameModel.Snapshot.WorldState.TurnInfo.CurrentPlayerId, Is.EqualTo(ai.Id));
    }

    [Test]
    public async Task AiTurnAfterGameStopTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 60, damage: 5) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("AiStack"),
            ai.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("HumanStack"),
            human.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(aiStack)
            .WithStack(humanStack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Task task = gameViewModel.ExecuteAiTurnsAsync(world);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<BattleScreenViewModel>());
        Assert.That(task.IsCompleted, Is.False);

        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        Assert.That(task.IsCompleted, Is.True);
        Assert.That(gameViewModel.RootScreen, Is.InstanceOf<MainMenuScreenViewModel>());
    }

    [Test]
    public void InterfaceOpenSoundTest()
    {
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        OpenSoundScreen root = new(new AssetSoundKey.InterfaceOpen());
        OpenSoundScreen silent = new(null);
        OpenSoundScreen opened = new(new AssetSoundKey.BookOpen());
        OpenSoundScreen replaced = new(null);

        gameViewModel.ShowScreen(root);
        gameViewModel.OpenScreen(silent);
        gameViewModel.OpenScreen(opened);
        gameViewModel.CloseScreen(opened);
        gameViewModel.ShowScreen(replaced);

        Assert.That(
            audioPlayer.Actions,
            Is.EqualTo(
            [
                "Sound:interface-open",
                "Sound:book-open"
            ]));
    }

    [Test]
    public async Task TurnAutoSaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioId(new ScenarioId("scenario-1"))
            .BuildSession();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameSaveStore saveStore = new(sb.GetPath("saves"), 2);

        await saveStore.SaveAsync(
            new GameSaveId(1),
            new GameSaveMetadata("Manual save", GameSaveType.Manual, "scenario-1", "scenario-1:NAME:0", 2, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            session.World.State);
        await saveStore.SaveAsync(
            new GameSaveId(2),
            new GameSaveMetadata("Autosave", GameSaveType.Turn, "scenario-2", "Scenario Two", 5, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"),
            session.World.State);

        using GameViewModel gameViewModel = new TestGameViewModelBuilder()
            .WithSaveStore(saveStore)
            .WithSettings(GameSettings.Create(100, 100, false, 100, 3, 2, BattleSpeed.Normal, null, true))
            .Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        await world.EndTurnCommand.ExecuteAsync(null);
        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        LoadGameScreenViewModel firstLoadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(
            FormatSaves(firstLoadScreen.SaveList.Saves),
            Is.EqualTo(["Autosave | Scenario Two | Turn 5", "Manual save | scenario-1:NAME:0 | Turn 2"]));

        gameViewModel.EscapeCommand.Execute(null);

        await world.EndTurnCommand.ExecuteAsync(null);
        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        LoadGameScreenViewModel secondLoadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(
            FormatSaves(secondLoadScreen.SaveList.Saves),
            Is.EqualTo([
                "Autosave | scenario-1:NAME:0 | Turn 3",
                "Autosave | Scenario Two | Turn 5",
                "Manual save | scenario-1:NAME:0 | Turn 2"
            ]));

        gameViewModel.EscapeCommand.Execute(null);

        await world.EndTurnCommand.ExecuteAsync(null);
        await world.EndTurnCommand.ExecuteAsync(null);
        await world.EndTurnCommand.ExecuteAsync(null);
        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        LoadGameScreenViewModel thirdLoadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);
        GameSave saved = Assert.NotNull(await saveStore.TryLoadAsync(new GameSaveId(4)));

        Assert.That(
            FormatSaves(thirdLoadScreen.SaveList.Saves),
            Is.EqualTo([
                "Autosave | scenario-1:NAME:0 | Turn 6",
                "Autosave | scenario-1:NAME:0 | Turn 3",
                "Manual save | scenario-1:NAME:0 | Turn 2"
            ]));
        Assert.That(
            $"{saved.Metadata.Name} | {saved.Metadata.Type} | {saved.Metadata.ScenarioId} | {saved.Metadata.ScenarioName} | {saved.Metadata.TurnNumber}",
            Is.EqualTo("Autosave | Turn | scenario-1 | scenario-1:NAME:0 | 6"));
        Assert.That(saved.WorldState, Is.EqualTo(session.World.State));
    }

    [Test]
    public async Task NeverAutoSaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithTurnInfo(new WorldTurnInfo(9, player.Id))
            .BuildSession();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder()
            .WithSaveStore(new GameSaveStore(sb.GetPath("saves"), 10))
            .WithSettings(GameSettings.Create(100, 100, false, 100, 0, 10, BattleSpeed.Normal, null, true))
            .Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        await world.EndTurnCommand.ExecuteAsync(null);
        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        LoadGameScreenViewModel loadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(session.World.State.TurnInfo, Is.EqualTo(new WorldTurnInfo(10, player.Id)));
        Assert.That(loadScreen.SaveList.Saves, Is.Empty);
    }

    [Test]
    public void ShowErrorTest()
    {
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();

        gameViewModel.ShowError("Not enough resources");

        Alert first = Assert.NotNull(gameViewModel.Alert.Current);

        gameViewModel.ShowError("Not enough resources");

        Alert second = Assert.NotNull(gameViewModel.Alert.Current);

        Assert.That(first, Is.EqualTo(new Alert(AlertType.Error, IconMoniker.Warning, "Not enough resources", Details: null)));
        Assert.That(second, Is.EqualTo(new Alert(AlertType.Error, IconMoniker.Warning, "Not enough resources", Details: null)));
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(audioPlayer.Actions, Is.EqualTo(["Sound:beep", "Sound:beep"]));
    }

    [Test]
    public async Task IncompatibleSaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);

        sb.CreateDirectory("saves");

        await using (FileStream stream = File.Create(sb.GetPath(Path.Combine("saves", "save-1.json.gz"))))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write("""{"formatVersion":34,"metadata":{"name":"Old campaign","type":"Manual","scenarioId":"S137SC0000","scenarioName":"Conquest of Thelena","turnNumber":14,"savedAt":"2026-01-02T03:04:05+00:00","savedBy":"0.0.9+1c2b3a4d5e6f"},"worldState":{}}"""u8);
        }

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithSaveStore(saveStore).Build();
        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        MainMenuScreenViewModel menu = Assert.IsOfType<MainMenuScreenViewModel>(gameViewModel.RootScreen);

        await gameViewModel.ShowLoadGameCommand.ExecuteAsync(null);

        LoadGameScreenViewModel loadScreen = Assert.IsOfType<LoadGameScreenViewModel>(gameViewModel.TopScreen);
        GameSaveViewModel save = Assert.NotNull(loadScreen.SaveList.SelectedSave);
        GameSaveViewModel latestSave = Assert.NotNull(menu.LatestSave);

        Assert.That(latestSave.Entry, Is.Null);
        Assert.That(FormatSaves(loadScreen.SaveList.Saves), Is.EqualTo(["Old campaign | Incompatible game save from 0.0.9+1c2b3a4d5 | Turn 14"]));
        Assert.That(gameViewModel.LoadGameCommand.CanExecute(save.Entry), Is.False);
    }

    [Test]
    public async Task AiTurnBattleAutoResolvedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 60, damage: 5) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Neutral, map) with { IsAi = true };

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("HumanStack"),
            human.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack nearAiStack = TestHelpers.SimpleStack(
            new WorldStackId("NearAiStack"),
            ai.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack farAiStack = TestHelpers.SimpleStack(
            new WorldStackId("FarAiStack"),
            ai.Id,
            new WorldPosition(4, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker2"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack neutralStack = TestHelpers.SimpleStack(
            new WorldStackId("NeutralStack"),
            neutral.Id,
            new WorldPosition(7, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender2"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(humanStack)
            .WithStack(nearAiStack)
            .WithStack(farAiStack)
            .WithStack(neutralStack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Task task = gameViewModel.ExecuteAiTurnsAsync(world);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.RenderRequest.Animation, Is.InstanceOf<WorldAnimationState.StackMove>());

        world.CompleteAnimation();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.StackDestroy(new WorldPosition(7, 0), defenderDefinition.Id)));

        world.CompleteAnimation();

        BattleScreenViewModel battleScreen = Assert.IsOfType<BattleScreenViewModel>(gameViewModel.TopScreen);
        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.BattleStart(new WorldPosition(0, 0))));

        await battleScreen.CompleteTransitionAsync();
        battleScreen.AutoResolveCommand.Execute(null);

        ConfirmPromptViewModel autoResolvePrompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await autoResolvePrompt.ConfirmCommand.ExecuteAsync(null);

        battleScreen.ExitBattleCommand.Execute(null);
        await battleScreen.CompleteTransitionAsync();

        Assert.That(task.IsCompleted, Is.True);
        Assert.That(gameModel.Snapshot.BattleState, Is.Null);
    }

    [Test]
    public async Task SaveGameSameNameTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioId(new ScenarioId("scenario-1"))
            .BuildSession();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameSaveStore saveStore = new(sb.GetPath("saves"), 10);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithSaveStore(saveStore).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        gameViewModel.EscapeCommand.Execute(null);

        GameMenuViewModel menu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);

        await menu.SaveGameCommand.ExecuteAsync(null);

        SaveGameScreenViewModel saveScreen = Assert.IsOfType<SaveGameScreenViewModel>(gameViewModel.TopScreen);

        await saveScreen.SaveCommand.ExecuteAsync(null);

        Assert.That(saveScreen.SaveList.SelectedSave, Is.EqualTo(saveScreen.SaveList.Saves[0]));
        Assert.That(saveScreen.SaveLabel, Is.EqualTo("Overwrite"));

        await saveScreen.SaveCommand.ExecuteAsync(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        Assert.That(prompt.Message, Is.EqualTo(TextSpans.FromText("Replace \"scenario-1:NAME:0, turn 1\" with the current game?")));

        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(FormatSaves(saveScreen.SaveList.Saves), Is.EqualTo(["scenario-1:NAME:0, turn 1 | scenario-1:NAME:0 | Turn 1"]));
        Assert.That(sb.GetTree(), Is.EquivalentTo(["saves/", "saves/save-1.json.gz"]));

        saveScreen.Name = "Another save";

        Assert.That(saveScreen.SaveList.SelectedSave, Is.Null);
        Assert.That(saveScreen.SaveLabel, Is.EqualTo("Save"));

        saveScreen.Name = " scenario-1:NAME:0, turn 1 ";

        Assert.That(saveScreen.SaveList.SelectedSave, Is.EqualTo(saveScreen.SaveList.Saves[0]));
        Assert.That(saveScreen.SaveLabel, Is.EqualTo("Overwrite"));
    }

    [Test]
    public async Task GameSelectOpenSoundTest()
    {
        TestAudioPlayer audioPlayer = new();
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();

        await gameViewModel.ShowCampaignSelectCommand.ExecuteAsync(null);
        await gameViewModel.ShowScenarioSelectCommand.ExecuteAsync(null);

        Assert.That(audioPlayer.Actions, Is.EqualTo([
            "Sound:interface-open",
            "Music:menu-music from track 0",
            "Sound:interface-open"
        ]));
    }
}
