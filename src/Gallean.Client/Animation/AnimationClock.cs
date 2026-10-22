using System.Diagnostics;

namespace Gallean.Client;

internal sealed class AnimationClock
{
    private static readonly TimeSpan MaxTickDelta = TimeSpan.FromSeconds(0.1);

    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private TimeSpan lastTick;

    public TimeSpan Now => stopwatch.Elapsed;

    public TimeSpan Tick()
    {
        TimeSpan now = stopwatch.Elapsed;
        TimeSpan delta = now - lastTick;

        lastTick = now;

        if (delta > MaxTickDelta)
        {
            delta = MaxTickDelta;
        }

        return delta;
    }
}
