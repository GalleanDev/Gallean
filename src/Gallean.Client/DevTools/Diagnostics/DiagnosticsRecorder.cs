using System.Diagnostics;

namespace Gallean.Client;

internal sealed partial class DiagnosticsRecorder
{
    public readonly record struct Frame(TimeSpan Interval, bool Continuous);

    public const int FrameCapacity = 180;

    private const double BytesPerMegabyte = 1_000_000;

    private static readonly TimeSpan ReportInterval = TimeSpan.FromMilliseconds(500.0);

    private readonly Lock locker = new();
    private readonly FrameLog frameLog = new();
    private readonly TimingAccumulator frameTiming = new();
    private readonly TimingAccumulator drawTiming = new();
    private readonly TimingAccumulator loopTiming = new();
    private readonly TimingAccumulator pointerTiming = new();
    private readonly TimingAccumulator buildTiming = new();

    private DiagnosticsReport report = DiagnosticsReport.Empty;
    private TimeSpan budget = DiagnosticsReport.DefaultBudget;

    private long loopFrames;
    private long presentedFrames;
    private long draws;
    private long builds;
    private long pointerMoves;
    private long hoverChanges;
    private int hitTargets;

    private long windowTimestamp;
    private long windowAllocatedBytes;
    private int windowGen0;
    private int windowGen1;
    private int windowGen2;

    public bool IsEnabled { get; set; }

    public void AddFrame(TimeSpan interval, bool continuous)
    {
        lock (locker)
        {
            frameLog.Add(new Frame(interval, continuous));
            loopFrames++;

            if (continuous)
            {
                frameTiming.Add(interval);
                presentedFrames++;
            }
        }
    }

    public void AddLoop(TimeSpan elapsed)
    {
        lock (locker)
        {
            loopTiming.Add(elapsed);
        }
    }

    public void AddDraw(TimeSpan elapsed)
    {
        lock (locker)
        {
            drawTiming.Add(elapsed);
            draws++;
        }
    }

    public void AddBuild(TimeSpan elapsed)
    {
        lock (locker)
        {
            buildTiming.Add(elapsed);
            builds++;
        }
    }

    public void AddPointerMove(TimeSpan elapsed, int targets)
    {
        lock (locker)
        {
            pointerTiming.Add(elapsed);
            pointerMoves++;
            hitTargets = targets;
        }
    }

    public void AddHoverChange()
    {
        lock (locker)
        {
            hoverChanges++;
        }
    }

    public DiagnosticsReport Capture()
    {
        lock (locker)
        {
            long now = Stopwatch.GetTimestamp();

            if (windowTimestamp == 0)
            {
                StartWindow(now);
            }

            TimeSpan window = Stopwatch.GetElapsedTime(windowTimestamp, now);

            if (window >= ReportInterval)
            {
                RollWindow(window, now);
            }

            return report;
        }
    }

    public int CopyFrames(Span<Frame> destination)
    {
        lock (locker)
        {
            return frameLog.CopyTo(destination);
        }
    }

    private void StartWindow(long nowTimestamp)
    {
        windowTimestamp = nowTimestamp;
        windowAllocatedBytes = GC.GetTotalAllocatedBytes();
        windowGen0 = GC.CollectionCount(0);
        windowGen1 = GC.CollectionCount(1);
        windowGen2 = GC.CollectionCount(2);
    }

    private void RollWindow(TimeSpan window, long nowTimestamp)
    {
        long allocatedBytes = GC.GetTotalAllocatedBytes();
        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        double seconds = window.TotalSeconds;

        budget = frameLog.EstimateBudget(budget);

        report = new DiagnosticsReport
        {
            Budget = budget,
            LoopFramesPerSecond = loopFrames / seconds,
            PresentedFramesPerSecond = presentedFrames / seconds,
            Frame = frameTiming.Capture(),
            Draw = drawTiming.Capture(),
            Loop = loopTiming.Capture(),
            Pointer = pointerTiming.Capture(),
            Build = buildTiming.Capture(),
            DrawsPerSecond = draws / seconds,
            BuildsPerSecond = builds / seconds,
            PointerMovesPerSecond = pointerMoves / seconds,
            HoverChangesPerSecond = hoverChanges / seconds,
            HitTargets = hitTargets,
            Gen0Collections = gen0 - windowGen0,
            Gen1Collections = gen1 - windowGen1,
            Gen2Collections = gen2 - windowGen2,
            AllocatedMegabytesPerSecond = (allocatedBytes - windowAllocatedBytes) / BytesPerMegabyte / seconds,
            HeapMegabytes = GC.GetTotalMemory(false) / BytesPerMegabyte,
            WorkingSetMegabytes = Environment.WorkingSet / BytesPerMegabyte
        };

        frameTiming.Reset();
        drawTiming.Reset();
        loopTiming.Reset();
        pointerTiming.Reset();
        buildTiming.Reset();

        loopFrames = 0;
        presentedFrames = 0;
        draws = 0;
        builds = 0;
        pointerMoves = 0;
        hoverChanges = 0;

        windowTimestamp = nowTimestamp;
        windowAllocatedBytes = allocatedBytes;
        windowGen0 = gen0;
        windowGen1 = gen1;
        windowGen2 = gen2;
    }
}
