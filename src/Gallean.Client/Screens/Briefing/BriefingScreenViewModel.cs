using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class BriefingScreenViewModel(
    string title,
    string briefing,
    ScenarioTextFormatter formatter,
    string objective,
    MenuBackgroundViewModel? background,
    AssetAnimationKey? video,
    AssetSoundKey? voice,
    IAudioPlayer audioPlayer,
    Func<Task> continueAction) : AbstractScreenViewModel
{
    private readonly MusicQueue musicQueue = MusicQueue.Create([new AssetSoundKey.BriefingMusic()], isLooping: true);
    private IAudioPlayback? voicePlayback;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleTextLabel))]
    public partial bool IsTextVisible { get; private set; } = true;

    public string Title { get; } = title;
    public ImmutableArray<ScenarioTextSpan> Briefing { get; } = formatter.Format(ScenarioFormatter.FormatBriefing(briefing));
    public string Objective { get; } = ScenarioFormatter.FormatObjective(objective);
    public MenuBackgroundViewModel? Background { get; } = background;
    public AssetAnimationKey? Video { get; } = video;
    public bool HasVideo { get; } = video is not null;
    public TimeSpan VideoStartTime { get; } = TimeSpan.FromSeconds(13.5);
    public bool HasVoice { get; } = voice is not null;
    public string ToggleTextLabel => IsTextVisible ? Tr("Hide text") : Tr("Show text");
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

    [RelayCommand]
    private void ToggleText()
    {
        IsTextVisible = !IsTextVisible;
    }
}
