using System.Collections.Immutable;
using System.Globalization;
using SkiaSharp;
using Frame = Gallean.Client.DiagnosticsRecorder.Frame;
using Timing = Gallean.Client.DiagnosticsReport.Timing;

namespace Gallean.Client;

internal sealed class DiagnosticsPanel
{
    public readonly record struct Line(string Text, SKColor Color);

    private const float HeaderHeight = 20;
    private const float HeaderBaseline = 12;
    private const float GraphHeight = 52;
    private const float GraphGap = 10;
    private const float GraphCornerRadius = 4;
    private const float BarOverlap = 0.5f;
    private const float BudgetThickness = 1;
    private const double GraphHeadroom = 2.5;
    private const float LineHeight = 16;
    private const float LineBaseline = 11;

    private const double MinimumLiveFramesPerSecond = 1;
    private const double BudgetWarningFactor = 1.25;
    private const double BudgetCriticalFactor = 2;
    private const int FrameTimingsAlignment = -18;
    private const double Gen0Warning = 3;
    private const double Gen0Critical = 10;
    private const double AllocatedWarningMegabytesPerSecond = 30;
    private const double AllocatedCriticalMegabytesPerSecond = 150;
    private const double WorkingSetWarningMegabytes = 4000;
    private const double WorkingSetCriticalMegabytes = 8000;

    private static readonly TimeSpan LoopWarning = TimeSpan.FromMilliseconds(4.0);
    private static readonly TimeSpan LoopCritical = TimeSpan.FromMilliseconds(10.0);
    private static readonly TimeSpan PointerWarning = TimeSpan.FromMilliseconds(3.0);
    private static readonly TimeSpan PointerCritical = TimeSpan.FromMilliseconds(8.0);
    private static readonly TimeSpan BuildWarning = TimeSpan.FromMilliseconds(6.0);
    private static readonly TimeSpan BuildCritical = TimeSpan.FromMilliseconds(14.0);

    private static readonly SKColor PlotFillColor = new(255, 255, 255, 12);
    private static readonly SKColor BudgetLineColor = new(150, 156, 168, 150);
    private static readonly SKColor IdleColor = new(92, 97, 107, 150);

    private static readonly SKTypeface MonoTypeface = ResolveMonospace(SKFontStyle.Normal);
    private static readonly SKTypeface MonoBoldTypeface = ResolveMonospace(SKFontStyle.Bold);

    private readonly SKFont badgeFont = new(MonoBoldTypeface, 13.5f);
    private readonly SKFont bodyFont = new(MonoTypeface, 12);
    private readonly SKPaint paint = new() { IsAntialias = true };
    private readonly Frame[] frames = new Frame[DiagnosticsRecorder.FrameCapacity];
    private DiagnosticsReport? formattedReport;
    private Line badge;
    private ImmutableArray<Line> lines = [];

    public DiagnosticsPanel()
    {
        Size = Measure();
    }

    public static SKColor TextColor { get; } = new(228, 231, 236, 236);
    public static SKColor MutedColor { get; } = new(148, 154, 165, 220);
    public static SKColor GoodColor { get; } = new(96, 202, 136, 236);
    public static SKColor WarningColor { get; } = new(236, 194, 92, 236);
    public static SKColor CriticalColor { get; } = new(236, 98, 86, 246);

    public SKSize Size { get; }

    public static Line FormatBadge(DiagnosticsReport report)
    {
        if (IsLive(report))
        {
            string text = string.Create(CultureInfo.InvariantCulture, $"{report.PresentedFramesPerSecond:0} FPS");

            return new Line(text, FrameColor(report.Frame.Maximum, report.Budget));
        }
        else
        {
            return new Line("IDLE", MutedColor);
        }
    }

    public static ImmutableArray<Line> FormatLines(DiagnosticsReport report)
    {
        return
        [
            FormatFrame(report),
            FormatDraw(report),
            FormatLoop(report),
            FormatPointer(report),
            FormatInput(report),
            FormatBuild(report),
            FormatCollections(report),
            FormatAllocations(report),
            FormatWorkingSet(report)
        ];
    }

    public void Draw(SKCanvas canvas, SKRect bounds, DiagnosticsRecorder recorder)
    {
        DiagnosticsReport report = recorder.Capture();
        int frameCount = recorder.CopyFrames(frames);

        Format(report);
        DrawBadge(canvas, bounds.Left, bounds.Top + HeaderBaseline);

        float graphTop = bounds.Top + HeaderHeight + GraphGap;
        SKRect graph = new(bounds.Left, graphTop, bounds.Right, graphTop + GraphHeight);

        DrawGraph(canvas, graph, frameCount, report.Budget);
        DrawLines(canvas, bounds.Left, graph.Bottom + GraphGap + LineBaseline);
    }

    private static SKTypeface ResolveMonospace(SKFontStyle style)
    {
        string[] families = ["Consolas", "Cascadia Mono", "DejaVu Sans Mono", "Liberation Mono", "Menlo", "Courier New"];

        foreach (string family in families)
        {
            SKTypeface candidate = SKTypeface.FromFamilyName(family, style);

            if (candidate is not null && candidate.FamilyName.Equals(family, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return SKTypeface.Default;
    }

    private static bool IsLive(DiagnosticsReport report)
    {
        return report.PresentedFramesPerSecond >= MinimumLiveFramesPerSecond;
    }

    public static SKColor FrameColor(TimeSpan interval, TimeSpan budget)
    {
        double milliseconds = interval.TotalMilliseconds;
        double budgetMilliseconds = budget.TotalMilliseconds;

        if (milliseconds <= budgetMilliseconds * BudgetWarningFactor)
        {
            return GoodColor;
        }
        else if (milliseconds <= budgetMilliseconds * BudgetCriticalFactor)
        {
            return WarningColor;
        }
        else
        {
            return CriticalColor;
        }
    }

    private static SKColor BarColor(Frame frame, TimeSpan budget)
    {
        if (frame.Continuous)
        {
            return FrameColor(frame.Interval, budget);
        }
        else
        {
            return IdleColor;
        }
    }

    private static SKColor ThresholdColor(TimeSpan value, TimeSpan warning, TimeSpan critical)
    {
        return ThresholdColor(value.TotalMilliseconds, warning.TotalMilliseconds, critical.TotalMilliseconds);
    }

    private static SKColor ThresholdColor(double value, double warning, double critical)
    {
        if (value >= critical)
        {
            return CriticalColor;
        }
        else if (value >= warning)
        {
            return WarningColor;
        }
        else
        {
            return TextColor;
        }
    }

    private static Line FormatFrame(DiagnosticsReport report)
    {
        if (IsLive(report))
        {
            Timing frame = report.Frame;
            string timings = string.Create(
                CultureInfo.InvariantCulture,
                $"{frame.Last.TotalMilliseconds,4:0.0}/{frame.Average.TotalMilliseconds,4:0.0}/{frame.Maximum.TotalMilliseconds,5:0.0} ms");

            return new Line(FormatFrameLine(timings, report.LoopFramesPerSecond), FrameColor(frame.Maximum, report.Budget));
        }
        else
        {
            return new Line(FormatFrameLine("idle", report.LoopFramesPerSecond), MutedColor);
        }
    }

    private static string FormatFrameLine(string timings, double loopFramesPerSecond)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"frame   {timings,FrameTimingsAlignment}   loop {loopFramesPerSecond,3:0} Hz");
    }

    private static Line FormatDraw(DiagnosticsReport report)
    {
        Timing draw = report.Draw;
        string text = string.Create(
            CultureInfo.InvariantCulture,
            $"draw    {draw.Average.TotalMilliseconds,4:0.0}/{draw.Maximum.TotalMilliseconds,5:0.0} ms   draws {report.DrawsPerSecond,3:0}   budget {report.Budget.TotalMilliseconds,4:0.0} ms");

        return new Line(text, TextColor);
    }

    private static Line FormatLoop(DiagnosticsReport report)
    {
        Timing loop = report.Loop;
        string text = string.Create(
            CultureInfo.InvariantCulture,
            $"ui loop {loop.Average.TotalMilliseconds,4:0.00}/{loop.Maximum.TotalMilliseconds,5:0.0} ms");

        return new Line(text, ThresholdColor(loop.Maximum, LoopWarning, LoopCritical));
    }

    private static Line FormatPointer(DiagnosticsReport report)
    {
        Timing pointer = report.Pointer;
        string text = string.Create(
            CultureInfo.InvariantCulture,
            $"pointer {pointer.Average.TotalMilliseconds,4:0.00}/{pointer.Maximum.TotalMilliseconds,5:0.0} ms   {report.HitTargets,3} targets");

        return new Line(text, ThresholdColor(pointer.Maximum, PointerWarning, PointerCritical));
    }

    private static Line FormatInput(DiagnosticsReport report)
    {
        string text = string.Create(
            CultureInfo.InvariantCulture,
            $"moves {report.PointerMovesPerSecond,4:0}   hover changes {report.HoverChangesPerSecond,4:0}");

        return new Line(text, TextColor);
    }

    private static Line FormatBuild(DiagnosticsReport report)
    {
        Timing build = report.Build;
        string text = string.Create(
            CultureInfo.InvariantCulture,
            $"builds {report.BuildsPerSecond,3:0}   build {build.Average.TotalMilliseconds,4:0.0}/{build.Maximum.TotalMilliseconds,5:0.0} ms");

        return new Line(text, ThresholdColor(build.Maximum, BuildWarning, BuildCritical));
    }

    private static Line FormatCollections(DiagnosticsReport report)
    {
        string text = string.Create(
            CultureInfo.InvariantCulture,
            $"gc  gen0 {report.Gen0Collections,3}  gen1 {report.Gen1Collections,3}  gen2 {report.Gen2Collections,3}");

        return new Line(text, ThresholdColor(report.Gen0Collections, Gen0Warning, Gen0Critical));
    }

    private static Line FormatAllocations(DiagnosticsReport report)
    {
        string text = string.Create(
            CultureInfo.InvariantCulture,
            $"alloc {report.AllocatedMegabytesPerSecond,6:0.0} MB   heap {report.HeapMegabytes,5:0} MB");
        SKColor color = ThresholdColor(
            report.AllocatedMegabytesPerSecond,
            AllocatedWarningMegabytesPerSecond,
            AllocatedCriticalMegabytesPerSecond);

        return new Line(text, color);
    }

    private static Line FormatWorkingSet(DiagnosticsReport report)
    {
        string text = string.Create(CultureInfo.InvariantCulture, $"memory {report.WorkingSetMegabytes,5:0} MB");
        SKColor color = ThresholdColor(report.WorkingSetMegabytes, WorkingSetWarningMegabytes, WorkingSetCriticalMegabytes);

        return new Line(text, color);
    }

    private SKSize Measure()
    {
        Line idleBadge = FormatBadge(DiagnosticsReport.Empty);
        ImmutableArray<Line> idleLines = FormatLines(DiagnosticsReport.Empty);
        float width = badgeFont.MeasureText(idleBadge.Text);

        foreach (Line line in idleLines)
        {
            width = Math.Max(width, bodyFont.MeasureText(line.Text));
        }

        float height = HeaderHeight + GraphGap + GraphHeight + GraphGap + (idleLines.Length * LineHeight);

        return new SKSize(MathF.Ceiling(width), height);
    }

    private void Format(DiagnosticsReport report)
    {
        if (ReferenceEquals(formattedReport, report))
        {
            return;
        }

        formattedReport = report;
        badge = FormatBadge(report);
        lines = FormatLines(report);
    }

    private void DrawBadge(SKCanvas canvas, float left, float baseline)
    {
        paint.Color = badge.Color;
        canvas.DrawText(badge.Text, left, baseline, SKTextAlign.Left, badgeFont, paint);
    }

    private void DrawGraph(SKCanvas canvas, SKRect bounds, int frameCount, TimeSpan budget)
    {
        using SKRoundRect plot = new(bounds, GraphCornerRadius);

        paint.Color = PlotFillColor;
        canvas.DrawRoundRect(plot, paint);

        if (frameCount == 0)
        {
            return;
        }

        double scale = budget.TotalMilliseconds * GraphHeadroom;

        for (int index = 0; index < frameCount; index++)
        {
            scale = Math.Max(scale, frames[index].Interval.TotalMilliseconds);
        }

        float barWidth = bounds.Width / DiagnosticsRecorder.FrameCapacity;

        canvas.Save();
        canvas.ClipRoundRect(plot, antialias: true);

        for (int index = 0; index < frameCount; index++)
        {
            Frame frame = frames[index];
            float barHeight = (float)(frame.Interval.TotalMilliseconds / scale) * bounds.Height;
            float barLeft = bounds.Left + (index * barWidth);

            paint.Color = BarColor(frame, budget);
            canvas.DrawRect(new SKRect(barLeft, bounds.Bottom - barHeight, barLeft + barWidth + BarOverlap, bounds.Bottom), paint);
        }

        float budgetTop = bounds.Bottom - ((float)(budget.TotalMilliseconds / scale) * bounds.Height);

        paint.Color = BudgetLineColor;
        canvas.DrawRect(new SKRect(bounds.Left, budgetTop, bounds.Right, budgetTop + BudgetThickness), paint);

        canvas.Restore();
    }

    private void DrawLines(SKCanvas canvas, float left, float baseline)
    {
        float top = baseline;

        foreach (Line line in lines)
        {
            paint.Color = line.Color;
            canvas.DrawText(line.Text, left, top, SKTextAlign.Left, bodyFont, paint);
            top += LineHeight;
        }
    }
}
