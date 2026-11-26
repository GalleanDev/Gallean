using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed partial class BarVisual : Control
{
    private const double SpikeRatio = 0.5;
    private const double EdgeThickness = 1.5;
    private const double BevelThickness = 1;
    private const double TextureOpacity = 0.45;

    public static readonly StyledProperty<AnimationRange> AnimationProperty =
        AvaloniaProperty.Register<BarVisual, AnimationRange>(nameof(Animation), AnimationRange.None);

    private readonly AnimationPropertyPlayer currentPlayer = new();
    private readonly AnimationPropertyPlayer maximumPlayer = new();

    static BarVisual()
    {
        AffectsRender<BarVisual>(
            CurrentProperty,
            MaximumProperty,
            GainProperty,
            GainOpacityProperty,
            TextProperty,
            FillProperty,
            GainFillProperty,
            TrackProperty,
            FillOverlayProperty,
            RelightProperty,
            OverlayProperty,
            TextureProperty,
            BevelProperty,
            EdgeProperty,
            ForegroundProperty,
            FontFamilyProperty,
            FontSizeProperty,
            FontWeightProperty,
            FontFeaturesProperty,
            OutlineProperty,
            OutlineThicknessProperty);
    }

    [GeneratedStyledProperty]
    public partial double Current { get; set; }

    [GeneratedStyledProperty]
    public partial double Maximum { get; set; }

    [GeneratedStyledProperty]
    public partial double Gain { get; set; }

    [GeneratedStyledProperty(DefaultValue = 1.0)]
    public partial double GainOpacity { get; set; }

    public AnimationRange Animation
    {
        get => GetValue(AnimationProperty);
        set => SetValue(AnimationProperty, value);
    }

    [GeneratedStyledProperty]
    public partial string? Text { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Fill { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? GainFill { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Track { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? FillOverlay { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Relight { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Overlay { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Texture { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Bevel { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Edge { get; set; }

    [GeneratedStyledProperty(AddOwnerFrom = typeof(TextElement))]
    public partial IBrush? Foreground { get; set; }

    [GeneratedStyledProperty(AddOwnerFrom = typeof(TextElement))]
    public partial FontFamily FontFamily { get; set; }

    [GeneratedStyledProperty(AddOwnerFrom = typeof(TextElement))]
    public partial double FontSize { get; set; }

    [GeneratedStyledProperty(AddOwnerFrom = typeof(TextElement))]
    public partial FontWeight FontWeight { get; set; }

    [GeneratedStyledProperty(AddOwnerFrom = typeof(TextElement))]
    public partial FontFeatureCollection? FontFeatures { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Outline { get; set; }

    [GeneratedStyledProperty]
    public partial double OutlineThickness { get; set; }

    private static Geometry Chevron(Rect rect)
    {
        StreamGeometry geometry = new();
        double spike = Math.Min(rect.Height * SpikeRatio, rect.Width / 2.0);
        double middle = rect.Top + (rect.Height / 2.0);

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new Point(rect.Left + spike, rect.Top), true);
            context.LineTo(new Point(rect.Right - spike, rect.Top));
            context.LineTo(new Point(rect.Right, middle));
            context.LineTo(new Point(rect.Right - spike, rect.Bottom));
            context.LineTo(new Point(rect.Left + spike, rect.Bottom));
            context.LineTo(new Point(rect.Left, middle));
            context.EndFigure(true);
        }

        return geometry;
    }

    private static void FillIfPresent(DrawingContext context, IBrush? brush, Rect rect)
    {
        if (brush is not null)
        {
            context.FillRectangle(brush, rect);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        Rect outer = new Rect(Bounds.Size).Deflate(EdgeThickness / 2.0);

        if (outer.Width <= 0 || outer.Height <= 0)
        {
            return;
        }

        Rect inner = outer.Deflate(EdgeThickness / 2.0);
        Geometry outline = Chevron(outer);
        Geometry face = Chevron(inner);
        double currentRatio = GetRatio(Current);
        Rect filled = new(inner.Left, inner.Top, inner.Width * currentRatio, inner.Height);
        Rect gained = new(filled.Right, inner.Top, inner.Width * (GetRatio(Current + Gain) - currentRatio), inner.Height);

        using (context.PushGeometryClip(face))
        {
            FillIfPresent(context, Track, inner);
            FillIfPresent(context, Fill, filled);
            DrawGain(context, gained);
            FillIfPresent(context, FillOverlay, filled);
            DrawTexture(context, inner);
            FillIfPresent(context, Relight, inner);
            FillIfPresent(context, Overlay, inner);

            IBrush? bevel = Bevel;

            if (bevel is not null)
            {
                context.DrawGeometry(null, new Pen(bevel, BevelThickness * 2, lineJoin: PenLineJoin.Miter), face);
            }
        }

        IBrush? edge = Edge;

        if (edge is not null)
        {
            context.DrawGeometry(null, new Pen(edge, EdgeThickness, lineJoin: PenLineJoin.Miter), outline);
        }

        DrawText(context);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == AnimationProperty && IsLoaded)
        {
            StartAnimation();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        StartAnimation();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        currentPlayer.Stop();
        maximumPlayer.Stop();
    }

    private void StartAnimation()
    {
        currentPlayer.Start(this, CurrentProperty, Animation.Current);
        maximumPlayer.Start(this, MaximumProperty, Animation.Maximum);
    }

    private void DrawGain(DrawingContext context, Rect rect)
    {
        if (rect.Width > 0)
        {
            using (context.PushOpacity(GainOpacity))
            {
                FillIfPresent(context, GainFill, rect);
                FillIfPresent(context, FillOverlay, rect);
            }
        }
    }

    private void DrawTexture(DrawingContext context, Rect inner)
    {
        IBrush? texture = Texture;

        if (texture is null)
        {
            return;
        }

        using (context.PushOpacity(TextureOpacity))
        {
            context.FillRectangle(texture, inner);
        }
    }

    private void DrawText(DrawingContext context)
    {
        Typeface typeface = new(FontFamily, FontStyle.Normal, FontWeight);
        FormattedText text = new(FormatText(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, FontSize, Foreground);
        text.SetFontFeatures(FontFeatures);
        Point origin = new((Bounds.Width - text.Width) / 2.0, (Bounds.Height - text.Height) / 2.0);
        Geometry? geometry = text.BuildGeometry(origin);

        if (geometry is null)
        {
            return;
        }

        IBrush? outline = Outline;

        if (outline is not null && OutlineThickness > 0)
        {
            context.DrawGeometry(null, new Pen(outline, OutlineThickness * 2, lineJoin: PenLineJoin.Round), geometry);
        }

        context.DrawGeometry(Foreground, null, geometry);
    }

    private string FormatText()
    {
        string? text = Text;

        if (text is not null)
        {
            return text;
        }
        else
        {
            return string.Create(CultureInfo.CurrentCulture, $"{Current:F0}/{Maximum:F0}");
        }
    }

    private double GetRatio(double value)
    {
        if (Maximum > 0)
        {
            return Math.Clamp(value / Maximum, 0, 1);
        }
        else
        {
            return 0;
        }
    }
}
