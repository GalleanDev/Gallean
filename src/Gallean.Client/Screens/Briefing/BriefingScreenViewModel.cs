using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class BriefingScreenViewModel(
    string title,
    string briefing,
    ScenarioTextFormatter formatter,
    string objective,
    MenuBackgroundViewModel? background,
    AssetSoundKey? voice,
    IAudioPlayer audioPlayer,
    Func<Task> continueAction) : AbstractScreenViewModel
{
    private readonly MusicQueue musicQueue = MusicQueue.Create([new AssetSoundKey.BriefingMusic()], isLooping: true);
    private IAudioPlayback? voicePlayback;

    public string Title { get; } = title;
    public ImmutableArray<ScenarioTextSpan> Briefing { get; } = formatter.Format(ScenarioFormatter.FormatBriefing(briefing));
    public string Objective { get; } = ScenarioFormatter.FormatObjective(objective);
    public MenuBackgroundViewModel? Background { get; } = background;
    public bool HasVoice { get; } = voice is not null;
    public override ScreenMusic Music => HasVoice ? ScreenMusic.None : ScreenMusic.Tracks(musicQueue);

    public override void Dispose()
    {
        voicePlayback?.Stop();
    }

    protected override void OnOpened()
    {
        if (voice is not null)
        {
            voicePlayback = audioPlayer.PlayVoice(voice);
        }
    }

    protected override bool TryClose()
    {
        if (ContinueCommand.CanExecute(null))
        {
            ContinueCommand.Execute(null);
        }

        return true;
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        await continueAction();
    }
}
