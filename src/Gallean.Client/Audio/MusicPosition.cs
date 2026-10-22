namespace Gallean.Client;

internal sealed record MusicPosition(int Track, TimeSpan? Time)
{
    public static MusicPosition Start { get; } = new(0, null);
}
