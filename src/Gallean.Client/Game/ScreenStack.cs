using System.Collections.ObjectModel;
using System.ComponentModel;
using Gallean.Common;

namespace Gallean.Client;

internal sealed class ScreenStack(IAudioPlayer audioPlayer) : IDisposable
{
    private readonly ObservableCollection<AbstractScreenViewModel> screens = [];
    private readonly Dictionary<MusicQueue, MusicPosition> musicPositions = [];
    private MusicQueue? musicQueue;
    private IMusicPlayback? musicPlayback;

    public IReadOnlyList<AbstractScreenViewModel> Screens => screens;
    public AbstractScreenViewModel? Root => screens.FirstOrDefault();
    public AbstractScreenViewModel? Top => screens.LastOrDefault();

    public bool PaintsOwnBackground
    {
        get
        {
            AbstractScreenViewModel? coverScreen = screens.LastOrDefault(screen => screen.PaintsOwnBackground || screen.BlurBackdrop);

            return coverScreen is { PaintsOwnBackground: true };
        }
    }

    public event EventHandler? Changed;

    public void Dispose()
    {
        RemoveAbove(0, keep: null).Dispose();
        musicPlayback?.Stop();
    }

    public void Reset(AbstractScreenViewModel screen)
    {
        if (screens.Count == 1 && ReferenceEquals(screens[0], screen))
        {
            return;
        }

        using CleanupList removed = RemoveAbove(0, keep: screen);

        Attach(screen);
        PlayOpenSound(screen);
        OnChanged();

        musicPositions.Clear();
    }

    public void Open(AbstractScreenViewModel screen)
    {
        if (!screens.Contains(screen))
        {
            Attach(screen);
            PlayOpenSound(screen);
            OnChanged();
        }
    }

    public void Close(AbstractScreenViewModel screen)
    {
        int index = screens.IndexOf(screen);

        if (index >= 0)
        {
            if (screen.CloseSound is not null)
            {
                audioPlayer.PlaySound(screen.CloseSound);
            }

            using CleanupList removed = RemoveAbove(index, keep: null);

            OnChanged();
        }
        else
        {
            if (screen.Host is null)
            {
                screen.Dispose();
            }
        }
    }

    private void Attach(AbstractScreenViewModel screen)
    {
        screen.AttachHost(this);
        screen.PropertyChanged += OnScreenPropertyChanged;
        screens.Add(screen);
    }

    private void PlayOpenSound(AbstractScreenViewModel screen)
    {
        if (screen.OpenSound is not null)
        {
            audioPlayer.PlaySound(screen.OpenSound);
        }
    }

    private CleanupList RemoveAbove(int index, AbstractScreenViewModel? keep)
    {
        CleanupList removed = new();

        while (screens.Count > index)
        {
            AbstractScreenViewModel screen = screens[^1];

            screens.RemoveAt(screens.Count - 1);
            screen.PropertyChanged -= OnScreenPropertyChanged;
            screen.DetachHost();

            if (!ReferenceEquals(screen, keep))
            {
                removed.Add(screen);
            }
        }

        return removed;
    }

    private void OnChanged()
    {
        UpdateActiveMusic();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateActiveMusic()
    {
        ScreenMusic? activeMusic = ResolveActiveMusic();

        if (activeMusic is not null && !Equals(activeMusic.Queue, musicQueue))
        {
            if (musicQueue is not null && musicPlayback is not null)
            {
                musicPositions[musicQueue] = musicPlayback.Position;
                musicPlayback.Stop();
            }

            musicQueue = activeMusic.Queue;

            if (musicQueue is not null)
            {
                musicPlayback = PlayMusic(musicQueue);
            }
            else
            {
                musicPlayback = null;
            }
        }
    }

    private IMusicPlayback? PlayMusic(MusicQueue queue)
    {
        if (musicPositions.Remove(queue, out MusicPosition? position))
        {
            return audioPlayer.PlayMusic(queue, position);
        }
        else
        {
            return audioPlayer.PlayMusic(queue, MusicPosition.Start);
        }
    }

    private ScreenMusic? ResolveActiveMusic()
    {
        for (int index = screens.Count - 1; index >= 0; index--)
        {
            ScreenMusic music = screens[index].Music;

            if (!music.KeepCurrent)
            {
                return music;
            }
        }

        return null;
    }

    private void OnScreenPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AbstractScreenViewModel.Music))
        {
            UpdateActiveMusic();
        }
        else if (e.PropertyName == nameof(AbstractScreenViewModel.PaintsOwnBackground))
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
