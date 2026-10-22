namespace Gallean.Client;

internal sealed record DiagnosticsReport
{
    public readonly record struct Timing(TimeSpan Last, TimeSpan Average, TimeSpan Maximum);

    public static TimeSpan DefaultBudget { get; } = TimeSpan.FromSeconds(1.0 / 60.0);
    public static DiagnosticsReport Empty { get; } = new();

    public TimeSpan Budget { get; init; } = DefaultBudget;
    public double LoopFramesPerSecond { get; init; }
    public double PresentedFramesPerSecond { get; init; }

    public Timing Frame { get; init; }
    public Timing Draw { get; init; }
    public Timing Loop { get; init; }
    public Timing Pointer { get; init; }
    public Timing Build { get; init; }

    public double DrawsPerSecond { get; init; }
    public double BuildsPerSecond { get; init; }
    public double PointerMovesPerSecond { get; init; }
    public double HoverChangesPerSecond { get; init; }
    public int HitTargets { get; init; }

    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public double AllocatedMegabytesPerSecond { get; init; }
    public double HeapMegabytes { get; init; }
    public double WorkingSetMegabytes { get; init; }
}
