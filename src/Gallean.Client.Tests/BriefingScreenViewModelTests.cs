using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class BriefingScreenViewModelTests
{
    private sealed class TestAudioPlayer : IAudioPlayer
    {
        public List<string> Actions { get; } = [];
        public float SoundVolume { get; set; }
        public float MusicVolume { get; set; }

        public void PlaySound(AssetSoundKey key)
        {
        }

        public IMusicPlayback? PlayMusic(MusicQueue queue, MusicPosition position)
        {
            Actions.Add($"Music:{string.Join(',', queue.Tracks.Select(track => track.CanonicalId))}");

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
        public MusicPosition Position => MusicPosition.Start;

        public void Stop()
        {
            actions.Add($"Stop:{name}");
        }
    }

    [Test]
    public async Task StartGameTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.StartGameCommand.ExecuteAsync(new GameConfiguration("S108SC0000", Configuration: null));

        BriefingScreenViewModel briefing = Assert.IsOfType<BriefingScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(gameViewModel.RootScreen, Is.SameAs(briefing));
        Assert.That(briefing.Title, Is.EqualTo("The Alliance"));
        Assert.That(
            briefing.Briefing,
            Is.EqualTo(
            [
                new ScenarioTextSpan("During the harsh battles against Erhog, corrupted nobles, making sure nobility got their due, looted the ", ScenarioTextSpanType.Text),
                new ScenarioTextSpan("Empire", ScenarioTextSpanType.Faction),
                new ScenarioTextSpan("'s treasure. Many peasants had no food, and the fear of starvation forced them to follow petty agitators who claimed new leadership for the ", ScenarioTextSpanType.Text),
                new ScenarioTextSpan("Empire", ScenarioTextSpanType.Faction),
                new ScenarioTextSpan(".\n\nSoon, Nevendaar found itself on the brink of revolution. ", ScenarioTextSpanType.Text),
                new ScenarioTextSpan("Hubert de Lalye", ScenarioTextSpanType.Character),
                new ScenarioTextSpan(" proposed to replenish the ", ScenarioTextSpanType.Text),
                new ScenarioTextSpan("Empire", ScenarioTextSpanType.Faction),
                new ScenarioTextSpan("'s chests with his… personal belongings, in exchange for the royal crown of course. Evidently, many nobles accepted the offer,fearing the loss of their aristocratic privileges.\n\nThe ", ScenarioTextSpanType.Text),
                new ScenarioTextSpan("Empire", ScenarioTextSpanType.Faction),
                new ScenarioTextSpan(" was then divided. On one side of the schism were the virtuous nobles, who remained true to the Emperor Demosthene; opposing him, ", ScenarioTextSpanType.Text),
                new ScenarioTextSpan("Hubert de Lalye", ScenarioTextSpanType.Character),
                new ScenarioTextSpan(", the newly crowned prince of opportunism gathered his renegade forces. The only solution for the Emperor's followers was to seek the help of the ", ScenarioTextSpanType.Text),
                new ScenarioTextSpan("Mountain Clans", ScenarioTextSpanType.Faction),
                new ScenarioTextSpan(". ", ScenarioTextSpanType.Text),
                new ScenarioTextSpan("Empire", ScenarioTextSpanType.Faction),
                new ScenarioTextSpan(" emissaries were urged to seek Slookarijj Darkstone, member of the Clans counsel and friend of high king Morok Cloudkeeper.", ScenarioTextSpanType.Text)
            ]));
        Assert.That(briefing.Objective, Is.EqualTo("Find the Mountain Clans Diplomat Slookarijj"));
        Assert.That(briefing.Background?.SpriteKey, Is.EqualTo(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Human)));
        Assert.That(briefing.HasVoice, Is.False);
        Assert.That(briefing.Music.Queue?.Tracks, Is.EqualTo([new AssetSoundKey.BriefingMusic()]));
        Assert.That(briefing.Music.Queue?.IsLooping, Is.True);
    }

    [Test]
    public async Task ContinueTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.StartGameCommand.ExecuteAsync(new GameConfiguration("S108SC0000", Configuration: null));

        BriefingScreenViewModel briefing = Assert.IsOfType<BriefingScreenViewModel>(gameViewModel.TopScreen);

        await briefing.ContinueCommand.ExecuteAsync(null);

        Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        Assert.That(briefing.IsOpen, Is.False);
    }

    [Test]
    public async Task CloseTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.StartGameCommand.ExecuteAsync(new GameConfiguration("S108SC0000", Configuration: null));

        BriefingScreenViewModel briefing = Assert.IsOfType<BriefingScreenViewModel>(gameViewModel.TopScreen);

        gameViewModel.EscapeCommand.Execute(null);

        Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        Assert.That(briefing.IsOpen, Is.False);
    }

    [Test]
    public async Task NoBriefingTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.StartGameCommand.ExecuteAsync(new GameConfiguration(GameLoader.TutorialScenarioId, Configuration: null));

        Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
    }

    [Test]
    public async Task VoiceTest()
    {
        TestAssetProviderBuilder assetsBuilder = new();
        using TestAssetProvider assets = assetsBuilder
            .ResolveSprite(_ => null)
            .ResolveAnimation(_ => null)
            .ResolveSound(key => key.CanonicalId == "briefing-voice/s108sc0000" ? assetsBuilder.CreateSound(1, 2, 3) : null)
            .Build();
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder()
            .WithAssets(assets)
            .WithAudioPlayer(audioPlayer)
            .Build();

        await gameViewModel.StartGameCommand.ExecuteAsync(new GameConfiguration("S108SC0000", Configuration: null));

        BriefingScreenViewModel briefing = Assert.IsOfType<BriefingScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(briefing.HasVoice, Is.True);
        Assert.That(audioPlayer.Actions, Is.EqualTo(["Voice:briefing-voice/s108sc0000"]));
    }

    [Test]
    public async Task LeaveTest()
    {
        TestAssetProviderBuilder assetsBuilder = new();
        using TestAssetProvider assets = assetsBuilder
            .ResolveSprite(_ => null)
            .ResolveAnimation(_ => null)
            .ResolveSound(key => key.CanonicalId == "briefing-voice/s108sc0000" ? assetsBuilder.CreateSound(1, 2, 3) : null)
            .Build();
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder()
            .WithAssets(assets)
            .WithAudioPlayer(audioPlayer)
            .Build();

        await gameViewModel.StartGameCommand.ExecuteAsync(new GameConfiguration("S108SC0000", Configuration: null));

        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);

        Assert.That(audioPlayer.Actions, Is.EqualTo(
        [
            "Voice:briefing-voice/s108sc0000",
            "Music:menu-music",
            "Stop:briefing-voice/s108sc0000"
        ]));
    }
}
