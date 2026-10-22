namespace Gallean.Client;

internal interface IMusicPlayback : IAudioPlayback
{
    MusicPosition Position { get; }
}
