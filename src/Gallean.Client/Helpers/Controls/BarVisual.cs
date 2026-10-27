using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class BarVisual : Control
{
    private const double SpikeRatio = 0.5;
    private const double EdgeThickness = 1.5;
    private const double BevelThickness = 1;
    private const double TextureOpacity = 0.45;

    public static readonly StyledProperty<double> CurrentProperty =
        AvaloniaProperty.Register<BarVisual, double>(nameof(Current));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<BarVisual, double>(nameof(Maximum));

    public static readonly StyledProperty<double> GainProperty =
        AvaloniaProperty.Register<BarVisual, double>(nameof(Gain));

    public static readonly StyledProperty<double> GainOpacityProperty =
        AvaloniaProperty.Register<BarVisual, double>(nameof(GainOpacity), 1);

    public static readonly StyledProperty<IReadOnlyList<AnimationProperty>> AnimationProperty =
        AvaloniaProperty.Register<BarVisual, IReadOnlyList<AnimationProperty>>(nameof(Animation), []);

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<BarVisual, string?>(nameof(Text));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<BarVisual, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<IBrush?> GainFillProperty =
        AvaloniaProperty.Register<BarVisual, IBrush?>(nameof(GainFill));

    public static readonly StyledProperty<IBrush?> TrackProperty =
        AvaloniaProperty.Register<BarVisual, IBrush?>(nameof(Track));

    public static readonly StyledProperty<IBrush?> FillOverlayProperty =
        AvaloniaProperty.Register<BarVisual, IBrush?>(nameof(FillOverlay));

    public static readonly StyledProperty<IBrush?> RelightProperty =
        AvaloniaProperty.Register<BarVisual, IBrush?>(nameof(Relight));

    public static readonly StyledProperty<IBrush?> OverlayProperty =
        AvaloniaProperty.Register<BarVisual, IBrush?>(nameof(Overlay));

    public static readonly StyledProperty<IBrush?> TextureProperty =
        AvaloniaProperty.Register<BarVisual, IBrush?>(nameof(Texture));

    public static readonly StyledProperty<IBrush?> BevelProperty =
        AvaloniaProperty.Register<BarVisual, IBrush?>(nameof(Bevel));

    public static readonly StyledProperty<IBrush?> EdgeProperty =
        AvaloniaProperty.Register<BarVisual, IBrush?>(nameof(Edge));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<BarVisual>();

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner<BarVisual>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<BarVisual>();

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextElement.FontWeightProperty.AddOwner<BarVisual>();

    public static readonly StyledProperty<FontFeatureCollection?> FontFeaturesProperty =
        TextElement.FontFeaturesProperty.AddOwner<BarVisual>();

    public static readonly StyledProperty<IBrush?> OutlineProperty =
        AvaloniaProperty.Register<BarVisual, IBrush?>(nameof(Outline));

    public static readonly StyledProperty<double> OutlineThicknessProperty =
        AvaloniaProperty.Register<BarVisual, double>(nameof(OutlineThickness));

    private readonly AnimationPropertyPlayer player = new();

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

    public double Current
    {
        get => GetValue(CurrentProperty);
        set => SetValue(CurrentProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Gain
    {
        get => GetValue(GainProperty);
        set => SetValue(GainProperty, value);
    }

    public double GainOpacity
    {
        get => GetValue(GainOpacityProperty);
        set => SetValue(GainOpacityProperty, value);
    }

    public IReadOnlyList<AnimationProperty> Animation
    {
        get => GetValue(AnimationProperty);
        set => SetValue(AnimationProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? GainFill
    {
        get => GetValue(GainFillProperty);
        set => SetValue(GainFillProperty, value);
    }

    public IBrush? Track
    {
        get => GetValue(TrackProperty);
        set => SetValue(TrackProperty, value);
    }

    public IBrush? FillOverlay
    {
        get => GetValue(FillOverlayProperty);
        set => SetValue(FillOverlayProperty, value);
    }

    public IBrush? Relight
    {
        get => GetValue(RelightProperty);
        set => SetValue(RelightProperty, value);
    }

    public IBrush? Overlay
    {
        get => GetValue(OverlayProperty);
        set => SetValue(OverlayProperty, value);
    }

    public IBrush? Texture
    {
        get => GetValue(TextureProperty);
        set => SetValue(TextureProperty, value);
    }

    public IBrush? Bevel
    {
        get => GetValue(BevelProperty);
        set => SetValue(BevelProperty, value);
    }

    public IBrush? Edge
    {
        get => GetValue(EdgeProperty);
        set => SetValue(EdgeProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public FontFeatureCollection? FontFeatures
    {
        get => GetValue(FontFeaturesProperty);
        set => SetValue(FontFeaturesProperty, value);
    }

    public IBrush? Outline
    {
        get => GetValue(OutlineProperty);
        set => SetValue(OutlineProperty, value);
    }

    public double OutlineThickness
    {
        get => GetValue(OutlineThicknessProperty);
        set => SetValue(OutlineThicknessProperty, value);
    }

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
            player.Start(this, CurrentProperty, Animation);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        player.Start(this, CurrentProperty, Animation);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        player.Stop();
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
