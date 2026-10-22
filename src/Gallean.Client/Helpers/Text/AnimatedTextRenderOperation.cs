using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class AnimatedTextRenderOperation(
    IReadOnlyList<AnimatedTextSegmentFrame> frames,
    AnimatedTextSmokeFrame? smokeFrame,
    Color glow,
    Color smoke,
    Color outline,
    double outlineThickness,
    double glowBlur,
    TimeSpan time) : ICustomDrawOperation
{
    private const double BlurExtent = 3.0;
    private const double SharpBlur = 0.2;
    private const double TexelsPerEm = 80.0;
    private const double DissolveSoftness = 0.1;

    private static readonly SKSamplingOptions SmokeSampling = new(SKCubicResampler.Mitchell);
    private static readonly Lazy<AnimatedTextSegmentShaderProgram> SegmentProgram = new(BuildSegmentProgram);
    private static readonly Lazy<AnimatedTextSmokeShaderProgram> SmokeProgram = new(BuildSmokeProgram);

    public Rect Bounds { get; } = GetBounds(frames, smokeFrame, outlineThickness, glowBlur);

    public bool HitTest(Point point)
    {
        return false;
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return ReferenceEquals(this, other);
    }

    public void Dispose()
    {
    }

    public void Render(ImmediateDrawingContext context)
    {
        using ISkiaSharpApiLease lease = context.GetSkiaApiLease();
        using SKPaint paint = new() { IsAntialias = true };

        SKCanvas canvas = lease.SkCanvas;

        if (outline.A > 0)
        {
            using SKPaint outlinePaint = new()
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)(outlineThickness * 2),
                StrokeJoin = SKStrokeJoin.Round
            };

            foreach (AnimatedTextSegmentFrame frame in frames)
            {
                if (frame.Opacity > 0)
                {
                    RenderSegmentOutline(canvas, outlinePaint, frame, lease.CurrentOpacity);
                }
            }
        }

        foreach (AnimatedTextSegmentFrame frame in frames)
        {
            if (frame.Opacity > 0)
            {
                RenderSegment(canvas, paint, frame, lease.CurrentOpacity);
            }
        }

        if (smokeFrame is not null && smoke.A > 0)
        {
            RenderSmoke(canvas, paint, smokeFrame, lease.CurrentOpacity);
        }
    }

    private void RenderSegmentOutline(SKCanvas canvas, SKPaint paint, AnimatedTextSegmentFrame frame, double opacity)
    {
        using SKFont font = CreateFont(frame.GlyphRun);
        using SKTextBlob? blob = CreateTextBlob(frame.GlyphRun, font);

        if (blob is null)
        {
            return;
        }

        float baseline = (float)frame.GlyphRun.BaselineOrigin.Y;
        SKMatrix transform = frame.Transform.ToSKMatrix();
        SKColorF color = ToSKColorF(outline, opacity * frame.Opacity);
        using SKShader shader = BuildSegmentShader(color, frame, frame.Dissolve, DissolveSoftness);
        using SKMaskFilter? blurFilter = CreateBlurFilter(frame.Blur);

        paint.Shader = shader;
        paint.MaskFilter = blurFilter;
        paint.Color = SKColors.White;
        canvas.Save();
        canvas.Concat(in transform);
        canvas.DrawText(blob, 0, baseline, paint);
        canvas.Restore();
    }

    private void RenderSegment(SKCanvas canvas, SKPaint paint, AnimatedTextSegmentFrame frame, double opacity)
    {
        using SKFont font = CreateFont(frame.GlyphRun);
        using SKTextBlob? blob = CreateTextBlob(frame.GlyphRun, font);

        if (blob is null)
        {
            return;
        }

        float baseline = (float)frame.GlyphRun.BaselineOrigin.Y;
        SKMatrix transform = frame.Transform.ToSKMatrix();

        canvas.Save();
        canvas.Concat(in transform);

        if (frame.Glow > 0 && glow.A > 0)
        {
            using SKMaskFilter glowFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, (float)glowBlur);

            paint.Shader = null;
            paint.BlendMode = SKBlendMode.Plus;
            paint.MaskFilter = glowFilter;
            paint.Color = ToSKColor(glow, opacity * frame.Opacity * frame.Glow);
            canvas.DrawText(blob, 0, baseline, paint);
        }

        SKColorF color = Mix(smoke, frame.Foreground, frame.Tint, opacity * frame.Opacity);
        using SKShader shader = BuildSegmentShader(color, frame, frame.Dissolve, DissolveSoftness);
        using SKMaskFilter? blurFilter = CreateBlurFilter(frame.Blur);

        paint.BlendMode = SKBlendMode.SrcOver;
        paint.Shader = shader;
        paint.MaskFilter = blurFilter;
        paint.Color = SKColors.White;
        canvas.DrawText(blob, 0, baseline, paint);
        canvas.Restore();
    }

    private void RenderSmoke(SKCanvas canvas, SKPaint paint, AnimatedTextSmokeFrame frame, double opacity)
    {
        SKImageInfo info = new(frame.Columns, frame.Rows, SKColorType.Alpha8, SKAlphaType.Premul);
        SKMatrix transform = SKMatrix.CreateScaleTranslation(
            (float)(frame.Area.Width / frame.Columns),
            (float)(frame.Area.Height / frame.Rows),
            (float)frame.Area.X,
            (float)frame.Area.Y);
        using SKImage opacityImage = SKImage.FromPixelCopy(info, frame.OpacityPixels);
        using SKShader opacityShader = opacityImage.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, SmokeSampling, transform);
        using SKShader shader = BuildSmokeShader(opacityShader, ToSKColorF(smoke, opacity * frame.Opacity), frame.NoiseSize);

        paint.BlendMode = SKBlendMode.SrcOver;
        paint.Shader = shader;
        paint.MaskFilter = null;
        paint.Color = SKColors.White;
        canvas.DrawRect(frame.Area.ToSKRect(), paint);
    }

    private SKShader BuildSmokeShader(SKShader opacity, SKColorF color, double noiseSize)
    {
        using AnimatedTextSmokeShaderProgram.Editor editor = SmokeProgram.Value.Edit();

        editor.SetOpacity(opacity);
        editor.SetColor(color);
        editor.SetTime((float)time.TotalSeconds);
        editor.SetFrequency((float)(TexelsPerEm / noiseSize));

        return editor.Build();
    }

    private SKShader BuildSegmentShader(SKColorF color, AnimatedTextSegmentFrame frame, double threshold, double softness)
    {
        using AnimatedTextSegmentShaderProgram.Editor editor = SegmentProgram.Value.Edit();

        editor.SetColor(color);
        editor.SetSeed(new SKPoint((float)(frame.Seed.X * SmokeTexture.Size), (float)(frame.Seed.Y * SmokeTexture.Size)));
        editor.SetTime((float)time.TotalSeconds);
        editor.SetFrequency((float)(TexelsPerEm / frame.NoiseSize));
        editor.SetThreshold((float)threshold);
        editor.SetSoftness((float)softness);

        return editor.Build();
    }

    private static AnimatedTextSegmentShaderProgram BuildSegmentProgram()
    {
        AnimatedTextSegmentShaderProgram program = new();

        using AnimatedTextSegmentShaderProgram.Editor editor = program.Edit();

        editor.SetNoise(SmokeTexture.CreateShader());

        return program;
    }

    private static AnimatedTextSmokeShaderProgram BuildSmokeProgram()
    {
        AnimatedTextSmokeShaderProgram program = new();

        using AnimatedTextSmokeShaderProgram.Editor editor = program.Edit();

        editor.SetNoise(SmokeTexture.CreateShader());

        return program;
    }

    private static SKColorF Mix(Color from, Color to, double amount, double opacity)
    {
        Color mixed = Color.FromArgb(
            (byte)Math.Round(double.Lerp(from.A, to.A, amount)),
            (byte)Math.Round(double.Lerp(from.R, to.R, amount)),
            (byte)Math.Round(double.Lerp(from.G, to.G, amount)),
            (byte)Math.Round(double.Lerp(from.B, to.B, amount)));

        return ToSKColorF(mixed, opacity);
    }

    private static SKColorF ToSKColorF(Color color, double opacity)
    {
        float alpha = (float)Math.Clamp(color.A * opacity / 255.0, 0, 1);

        return new SKColorF(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, alpha);
    }

    private static SKMaskFilter? CreateBlurFilter(double blur)
    {
        if (blur > SharpBlur)
        {
            return SKMaskFilter.CreateBlur(SKBlurStyle.Normal, (float)blur);
        }
        else
        {
            return null;
        }
    }

    private static SKFont CreateFont(GlyphRun glyphRun)
    {
        SKTypeface typeface = SkiaTypefaceCache.Get(glyphRun.GlyphTypeface);

        return new SKFont(typeface, (float)glyphRun.FontRenderingEmSize)
        {
            Edging = SKFontEdging.Antialias,
            Hinting = SKFontHinting.Full,
            Subpixel = true,
            BaselineSnap = true,
            LinearMetrics = true
        };
    }

    private static SKTextBlob? CreateTextBlob(GlyphRun glyphRun, SKFont font)
    {
        IReadOnlyList<GlyphInfo> glyphs = glyphRun.GlyphInfos;
        ushort[] indices = new ushort[glyphs.Count];
        SKPoint[] positions = new SKPoint[glyphs.Count];
        double pen = 0;

        for (int index = 0; index < glyphs.Count; index++)
        {
            GlyphInfo glyph = glyphs[index];

            indices[index] = glyph.GlyphIndex;
            positions[index] = new SKPoint((float)(pen + glyph.GlyphOffset.X), (float)glyph.GlyphOffset.Y);
            pen += glyph.GlyphAdvance;
        }

        using SKTextBlobBuilder builder = new();
        SKPositionedRunBuffer run = builder.AllocatePositionedRun(font, glyphs.Count);

        run.SetGlyphs(indices);
        run.SetPositions(positions);

        return builder.Build();
    }

    private static SKColor ToSKColor(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp(Math.Round(color.A * opacity), byte.MinValue, byte.MaxValue);

        return new SKColor(color.R, color.G, color.B, alpha);
    }

    private static Rect GetBounds(
        IReadOnlyList<AnimatedTextSegmentFrame> frames,
        AnimatedTextSmokeFrame? smokeFrame,
        double outlineThickness,
        double glowBlur)
    {
        Rect bounds = default;

        foreach (AnimatedTextSegmentFrame frame in frames)
        {
            Rect glyphBounds = frame.GlyphRun.Bounds;
            Rect frameBounds = glyphBounds.TransformToAABB(frame.Transform);

            bounds = bounds.Union(frameBounds.Inflate(outlineThickness + (BlurExtent * Math.Max(frame.Blur, glowBlur))));
        }

        if (smokeFrame is not null)
        {
            bounds = bounds.Union(smokeFrame.Area);
        }

        return bounds;
    }
}
