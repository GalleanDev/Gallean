using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Gallean.Client;

internal sealed class AnimatedTextBlock : OutlinedTextBlock
{
    private const double FadeStart = 0.15;
    private const double FadeLength = 0.45;
    private const double TintStart = 0.25;
    private const double GlowPeak = 0.8;
    private const double DissolveFrom = 1.0;
    private const double DissolveTo = -0.15;
    private const double SmokeEmissionLength = 0.4;
    private const double SmokeMinSize = 30.0;
    private const double SmokeCellSize = 8.0;
    private const double SmokeAbsorption = 0.8;
    private const double SmokePuffRadius = 0.6;
    private const double SmokeDriftX = 0.5;
    private const double SmokeMarginX = 1.0;
    private const double SmokeMarginTop = 4.0;
    private const double SmokeMarginBottom = 0.8;
    private const int SmokeMaxSteps = 2;

    private static readonly TimeSpan SmokeStep = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
    private static readonly TimeSpan SmokeTail = TimeSpan.FromSeconds(2.4);
    private static readonly TimeSpan SmokeFade = TimeSpan.FromSeconds(1.2);

    public static readonly StyledProperty<AnimatedTextReveal?> RevealProperty =
        AvaloniaProperty.Register<AnimatedTextBlock, AnimatedTextReveal?>(nameof(Reveal));

    public static readonly StyledProperty<TimeSpan> RevealDelayProperty =
        AvaloniaProperty.Register<AnimatedTextBlock, TimeSpan>(nameof(RevealDelay));

    public static readonly StyledProperty<Color> GlowColorProperty =
        AvaloniaProperty.Register<AnimatedTextBlock, Color>(nameof(GlowColor));

    public static readonly StyledProperty<Color> SmokeColorProperty =
        AvaloniaProperty.Register<AnimatedTextBlock, Color>(nameof(SmokeColor));

    public static readonly DirectProperty<AnimatedTextBlock, TimeSpan> RevealEndProperty =
        AvaloniaProperty.RegisterDirect<AnimatedTextBlock, TimeSpan>(nameof(RevealEnd), block => block.RevealEnd);

    private IReadOnlyList<AnimatedTextSegment> segments = [];
    private TextLayout? segmentLayout;
    private TimeSpan revealSpan;
    private TimeSpan animationSpan;
    private Rect? smokeArea;
    private SmokeSimulation? smoke;
    private TimeSpan smokeTime;
    private TopLevel? topLevel;
    private TimeSpan? start;
    private TimeSpan elapsed;
    private bool frameRequested;
    private bool completed;

    static AnimatedTextBlock()
    {
        ClipToBoundsProperty.OverrideDefaultValue<AnimatedTextBlock>(false);
    }

    public AnimatedTextReveal? Reveal
    {
        get => GetValue(RevealProperty);
        set => SetValue(RevealProperty, value);
    }

    public TimeSpan RevealDelay
    {
        get => GetValue(RevealDelayProperty);
        set => SetValue(RevealDelayProperty, value);
    }

    public Color GlowColor
    {
        get => GetValue(GlowColorProperty);
        set => SetValue(GlowColorProperty, value);
    }

    public Color SmokeColor
    {
        get => GetValue(SmokeColorProperty);
        set => SetValue(SmokeColorProperty, value);
    }

    public TimeSpan RevealEnd
    {
        get;
        private set => SetAndRaise(RevealEndProperty, ref field, value);
    }

    protected override Type StyleKeyOverride => typeof(TextBlock);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == TextProperty || e.Property == InlinesProperty)
        {
            Restart();
        }
        else if (e.Property == RevealProperty)
        {
            ReleaseSegments();
            InvalidateArrange();
            Restart();
        }
        else if (e.Property == RevealDelayProperty)
        {
            RevealEnd = RevealDelay + revealSpan;
            smoke = null;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        topLevel = TopLevel.GetTopLevel(this);
        topLevel?.AddHandler(PointerPressedEvent, OnTopLevelPointerPressed, RoutingStrategies.Tunnel, true);
        topLevel?.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel, true);
        InvalidateArrange();
        Restart();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        topLevel?.RemoveHandler(PointerPressedEvent, OnTopLevelPointerPressed);
        topLevel?.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
        topLevel = null;
        ReleaseSegments();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Size size = base.ArrangeOverride(finalSize);

        if (!ReferenceEquals(segmentLayout, TextLayout))
        {
            RebuildSegments();
        }

        return size;
    }

    protected override void RenderTextLayout(DrawingContext context, Point origin)
    {
        AnimatedTextReveal? reveal = Reveal;

        if (!completed && reveal is not null && Foreground is ISolidColorBrush foreground)
        {
            RenderReveal(context, origin, reveal, foreground);
        }
        else
        {
            base.RenderTextLayout(context, origin);
        }
    }

    private void RenderReveal(DrawingContext context, Point origin, AnimatedTextReveal reveal, ISolidColorBrush foreground)
    {
        List<AnimatedTextSegment> finished = [];
        List<AnimatedTextSegmentFrame> frames = [];
        double noiseSize = GetSmokeSize();

        foreach (AnimatedTextSegment segment in segments)
        {
            double progress = elapsed.ToAnimationProgress(RevealDelay + segment.Start, reveal.Duration);

            if (progress >= 1)
            {
                finished.Add(segment);
            }
            else if (progress > 0)
            {
                ISolidColorBrush segmentForeground = segment.Foreground as ISolidColorBrush ?? foreground;

                frames.Add(MakeFrame(origin, segment, GetColor(segmentForeground), reveal, progress, noiseSize));
            }
        }

        RenderFinished(context, origin, finished, foreground);

        AnimatedTextSmokeFrame? smokeFrame = AdvanceSmoke(origin, reveal);

        if (frames.Count > 0 || smokeFrame is not null)
        {
            context.Custom(new AnimatedTextRenderOperation(
                frames,
                smokeFrame,
                GlowColor,
                SmokeColor,
                GetOutlineColor(),
                OutlineThickness,
                reveal.GlowBlur,
                elapsed));
        }
    }

    private void RenderFinished(DrawingContext context, Point origin, List<AnimatedTextSegment> finished, ISolidColorBrush foreground)
    {
        Pen? pen = CreateOutlinePen();

        if (pen is not null)
        {
            foreach (AnimatedTextSegment segment in finished)
            {
                using (context.PushTransform(Matrix.CreateTranslation(origin + segment.Origin)))
                {
                    context.DrawGeometry(null, pen, segment.GlyphRun.BuildGeometry());
                }
            }
        }

        foreach (AnimatedTextSegment segment in finished)
        {
            ISolidColorBrush segmentForeground = segment.Foreground as ISolidColorBrush ?? foreground;

            using (context.PushTransform(Matrix.CreateTranslation(origin + segment.Origin)))
            {
                context.DrawGlyphRun(segmentForeground, segment.GlyphRun);
            }
        }
    }

    private Color GetOutlineColor()
    {
        if (Outline is ISolidColorBrush outline && OutlineThickness > 0)
        {
            return GetColor(outline);
        }
        else
        {
            return Colors.Transparent;
        }
    }

    private AnimatedTextSmokeFrame? AdvanceSmoke(Point origin, AnimatedTextReveal reveal)
    {
        if (smokeArea is null)
        {
            return null;
        }

        if (elapsed < RevealDelay)
        {
            return null;
        }

        Rect area = smokeArea.Value;

        if (smoke is null || elapsed < smokeTime)
        {
            smoke = new SmokeSimulation(area.Size, SmokeCellSize);
            smokeTime = RevealDelay;
        }

        for (int step = 0; step < SmokeMaxSteps; step++)
        {
            if (smokeTime + SmokeStep <= elapsed)
            {
                EmitSmoke(smoke, area, reveal);
                smoke.Step(SmokeStep);
                smokeTime += SmokeStep;
            }
            else
            {
                break;
            }
        }

        if (smokeTime + SmokeStep <= elapsed)
        {
            smokeTime = elapsed;
        }

        TimeSpan fadeStart = RevealDelay + animationSpan - SmokeFade;
        double opacity = 1 - AnimationEasing.Smooth.Ease((elapsed - fadeStart) / SmokeFade);
        Rect placement = new(origin + area.TopLeft, smoke.Area);

        return new AnimatedTextSmokeFrame(
            smoke.CreateOpacity(SmokeAbsorption),
            smoke.DensityColumns,
            smoke.DensityRows,
            placement,
            opacity,
            GetSmokeSize());
    }

    private void EmitSmoke(SmokeSimulation simulation, Rect area, AnimatedTextReveal reveal)
    {
        TimeSpan emission = reveal.Duration * SmokeEmissionLength;
        double share = SmokeStep / emission;
        double radius = SmokePuffRadius * GetSmokeSize();

        foreach (AnimatedTextSegment segment in segments)
        {
            double progress = smokeTime.ToAnimationProgress(RevealDelay + segment.Start, emission);

            if (progress > 0 && progress < 1)
            {
                Point center = segment.Origin + segment.GlyphRun.Bounds.Center - area.TopLeft;
                Vector drift = new(segment.Scatter.X * reveal.SmokeLift * SmokeDriftX, -reveal.SmokeLift);
                double swirl = Math.Sign(segment.Spin) * reveal.SmokeSwirl;

                simulation.AddPuff(center, radius, reveal.Smoke * share, drift * share, swirl * share);
            }
        }
    }

    private double GetSmokeSize()
    {
        return Math.Max(FontSize, SmokeMinSize);
    }

    private static Color GetColor(ISolidColorBrush brush)
    {
        Color color = brush.Color;

        return Color.FromArgb((byte)Math.Round(color.A * brush.Opacity), color.R, color.G, color.B);
    }

    private static AnimatedTextSegmentFrame MakeFrame(
        Point origin,
        AnimatedTextSegment segment,
        Color foreground,
        AnimatedTextReveal reveal,
        double progress,
        double noiseSize)
    {
        double remaining = 1 - AnimationEasing.Decelerate.Ease(progress);
        double scale = 1 + ((reveal.Scale - 1) * remaining);
        double angle = Matrix.ToRadians(segment.Spin * reveal.Spin * remaining);
        Vector approach = ((segment.Scatter * reveal.Scatter) + new Vector(0, reveal.Rise)) * remaining;
        Vector offset = Rotate(approach, reveal.Curl * remaining);
        Point center = segment.GlyphRun.Bounds.Center;
        Point placement = origin + segment.Origin + center + offset;

        return new AnimatedTextSegmentFrame(
            segment.GlyphRun,
            foreground,
            segment.Seed,
            Place(center, scale, angle, placement),
            AnimationEasing.Smooth.Ease((progress - FadeStart) / FadeLength),
            reveal.Blur * remaining,
            double.Lerp(DissolveFrom, DissolveTo, AnimationEasing.Smooth.Ease(progress)),
            AnimationEasing.Smooth.Ease((progress - TintStart) / (1 - TintStart)),
            GetGlow(progress),
            noiseSize);
    }

    private static Matrix Place(Point center, double scale, double angle, Point placement)
    {
        return Matrix.CreateTranslation(-center.X, -center.Y) *
               Matrix.CreateScale(scale, scale) *
               Matrix.CreateRotation(angle) *
               Matrix.CreateTranslation(placement.X, placement.Y);
    }

    private static Vector Rotate(Vector vector, double angle)
    {
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);

        return new Vector((vector.X * cos) - (vector.Y * sin), (vector.X * sin) + (vector.Y * cos));
    }

    private static double GetGlow(double progress)
    {
        double rise = AnimationEasing.Smooth.Ease(progress / GlowPeak);
        double fall = AnimationEasing.Smooth.Ease((progress - GlowPeak) / (1 - GlowPeak));

        return rise * (1 - fall);
    }

    private void Restart()
    {
        start = null;
        elapsed = TimeSpan.Zero;
        smoke = null;
        smokeTime = TimeSpan.Zero;
        completed = Reveal is null;
        InvalidateVisual();
        RequestFrame();
    }

    private bool IsAnimationComplete()
    {
        return completed || elapsed >= RevealEnd || !IsEffectivelyVisible;
    }

    private void CompleteAnimation()
    {
        if (!completed)
        {
            completed = true;
            smoke = null;
            InvalidateVisual();
        }
    }

    private void OnTopLevelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsAnimationComplete())
        {
            e.Handled = true;
            CompleteAnimation();
        }
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsAnimationComplete())
        {
            e.Handled = true;
            CompleteAnimation();
        }
    }

    private void RequestFrame()
    {
        if (!frameRequested && !completed && topLevel is not null)
        {
            frameRequested = true;
            topLevel.RequestAnimationFrame(OnAnimationFrame);
        }
    }

    private void OnAnimationFrame(TimeSpan frameTime)
    {
        frameRequested = false;

        if (topLevel is not null && !completed)
        {
            start ??= frameTime;
            elapsed = frameTime - start.Value;

            if (segmentLayout is not null && elapsed >= RevealDelay + animationSpan)
            {
                completed = true;
                smoke = null;
            }

            InvalidateVisual();
            RequestFrame();
        }
    }

    private void RebuildSegments()
    {
        ReleaseSegments();

        AnimatedTextReveal? reveal = Reveal;
        TextLayout layout = TextLayout;

        if (reveal is not null)
        {
            segments = BuildSegments(layout, reveal);
        }

        segmentLayout = layout;
        (revealSpan, animationSpan) = GetSpans(segments, reveal);
        smokeArea = GetSmokeArea(segments, reveal);
        smoke = null;
        RevealEnd = RevealDelay + revealSpan;
    }

    private void ReleaseSegments()
    {
        foreach (AnimatedTextSegment segment in segments)
        {
            segment.GlyphRun.Dispose();
        }

        segments = [];
        segmentLayout = null;
        smokeArea = null;
        smoke = null;
    }

    private static (TimeSpan Reveal, TimeSpan Animation) GetSpans(IReadOnlyList<AnimatedTextSegment> segments, AnimatedTextReveal? reveal)
    {
        if (reveal is not null && segments.Count > 0)
        {
            TimeSpan revealSpan = segments.Max(segment => segment.Start) + reveal.Duration;

            return (revealSpan, revealSpan + GetSmokeTail(reveal));
        }
        else
        {
            return (TimeSpan.Zero, TimeSpan.Zero);
        }
    }

    private static TimeSpan GetSmokeTail(AnimatedTextReveal reveal)
    {
        if (reveal.Smoke > 0)
        {
            return SmokeTail;
        }
        else
        {
            return TimeSpan.Zero;
        }
    }

    private Rect? GetSmokeArea(IReadOnlyList<AnimatedTextSegment> built, AnimatedTextReveal? reveal)
    {
        if (reveal is null || reveal.Smoke <= 0 || built.Count == 0)
        {
            return null;
        }

        double size = GetSmokeSize();
        Rect extent = built[0].GlyphRun.Bounds.Translate(built[0].Origin);

        foreach (AnimatedTextSegment segment in built)
        {
            extent = extent.Union(segment.GlyphRun.Bounds.Translate(segment.Origin));
        }

        return extent.Inflate(new Thickness(SmokeMarginX * size, SmokeMarginTop * size, SmokeMarginX * size, SmokeMarginBottom * size));
    }

    private static List<AnimatedTextSegment> BuildSegments(TextLayout layout, AnimatedTextReveal reveal)
    {
        List<AnimatedTextSegment> segments = [];
        double top = 0;

        foreach (TextLine line in layout.TextLines)
        {
            double left = line.Start;

            foreach (TextRun run in line.TextRuns)
            {
                if (run is ShapedTextRun shaped)
                {
                    Point origin = new(left, top + line.Baseline - shaped.Baseline);

                    segments.AddRange(SplitRun(shaped, origin, reveal, segments.Count));
                }

                if (run is DrawableTextRun drawable)
                {
                    left += drawable.Size.Width;
                }
            }

            top += line.Height;
        }

        return segments;
    }

    private static List<AnimatedTextSegment> SplitRun(ShapedTextRun run, Point origin, AnimatedTextReveal reveal, int firstIndex)
    {
        IBrush? foreground = run.Properties.ForegroundBrush;
        double[] pens = GetPens(run.GlyphRun);
        List<AnimatedTextSegment> segments = [];

        foreach ((int first, int end) in GetSegmentRanges(run, reveal.SegmentType))
        {
            GlyphRun glyphRun = SliceGlyphRun(run, first, end);
            Point segmentOrigin = new(origin.X + pens[first], origin.Y);

            segments.Add(MakeSegment(glyphRun, foreground, segmentOrigin, reveal, firstIndex + segments.Count));
        }

        return segments;
    }

    private static double[] GetPens(GlyphRun glyphRun)
    {
        IReadOnlyList<GlyphInfo> glyphs = glyphRun.GlyphInfos;
        double[] pens = new double[glyphs.Count];
        double pen = glyphRun.BaselineOrigin.X;

        for (int index = 0; index < glyphs.Count; index++)
        {
            pens[index] = pen;
            pen += glyphs[index].GlyphAdvance;
        }

        return pens;
    }

    private static List<(int First, int End)> GetSegmentRanges(ShapedTextRun run, AnimatedTextSegmentType type)
    {
        IReadOnlyList<GlyphInfo> glyphs = run.GlyphRun.GlyphInfos;
        ReadOnlySpan<char> text = run.Text.Span;

        List<(int First, int End)> ranges = [];

        for (int index = 0; index < glyphs.Count; index++)
        {
            char ch = text[glyphs[index].GlyphCluster - glyphs[0].GlyphCluster];

            if (char.IsWhiteSpace(ch))
            {
                continue;
            }
            else if (type == AnimatedTextSegmentType.Word && ranges.Count > 0 && ranges[^1].End == index)
            {
                ranges[^1] = (ranges[^1].First, index + 1);
            }
            else
            {
                ranges.Add((index, index + 1));
            }
        }

        return ranges;
    }

    private static GlyphRun SliceGlyphRun(ShapedTextRun run, int first, int end)
    {
        GlyphRun source = run.GlyphRun;
        IReadOnlyList<GlyphInfo> glyphs = source.GlyphInfos;
        int runStart = glyphs[0].GlyphCluster;
        int clusterStart = glyphs[first].GlyphCluster;
        int clusterEnd = runStart + run.Text.Length;

        if (end < glyphs.Count)
        {
            clusterEnd = glyphs[end].GlyphCluster;
        }

        GlyphInfo[] slice = new GlyphInfo[end - first];

        for (int index = first; index < end; index++)
        {
            GlyphInfo glyph = glyphs[index];

            slice[index - first] = new GlyphInfo(glyph.GlyphIndex, glyph.GlyphCluster - clusterStart, glyph.GlyphAdvance, glyph.GlyphOffset);
        }

        string characters = run.Text.Span[(clusterStart - runStart)..(clusterEnd - runStart)].ToString();

        return new GlyphRun(
            source.GlyphTypeface,
            source.FontRenderingEmSize,
            characters.AsMemory(),
            slice,
            new Point(0, source.BaselineOrigin.Y));
    }

    private static AnimatedTextSegment MakeSegment(
        GlyphRun glyphRun,
        IBrush? foreground,
        Point origin,
        AnimatedTextReveal reveal,
        int index)
    {
        double direction = Math.Tau * GetRandom(index, 0);
        double distance = 0.55 + (0.45 * GetRandom(index, 1));
        double spin = (2 * GetRandom(index, 2)) - 1;
        TimeSpan start = (reveal.Stagger * index) + (reveal.Jitter * GetRandom(index, 3));
        Vector scatter = new Vector(Math.Cos(direction), Math.Sin(direction)) * distance;
        Vector seed = new(GetRandom(index, 4), GetRandom(index, 5));

        return new AnimatedTextSegment(glyphRun, foreground, origin, start, scatter, spin, seed);
    }

    private static double GetRandom(int index, int channel)
    {
        return StableHash.Combine(index, channel) / (double)int.MaxValue;
    }
}
