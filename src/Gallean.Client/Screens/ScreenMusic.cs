namespace Gallean.Client;

internal abstract record ScreenMusic
{
    private sealed record NoneMusic : ScreenMusic;

    private sealed record KeepMusic : ScreenMusic;

    private sealed record TrackMusic(MusicQueue Value) : ScreenMusic;

    public static ScreenMusic None { get; } = new NoneMusic();
    public static ScreenMusic Keep { get; } = new KeepMusic();

    public bool KeepCurrent => this is KeepMusic;
    public MusicQueue? Queue => this is TrackMusic track ? track.Value : null;

    public static ScreenMusic Tracks(MusicQueue queue)
    {
        return new TrackMusic(queue);
    }
}
