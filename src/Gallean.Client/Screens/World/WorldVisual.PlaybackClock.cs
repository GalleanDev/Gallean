namespace Gallean.Client;

internal sealed partial class WorldVisual
{
    private sealed class PlaybackClock
    {
        public TimeSpan Now { get; private set; }

        public void Advance(TimeSpan delta)
        {
            Now += delta;
        }
    }
}
