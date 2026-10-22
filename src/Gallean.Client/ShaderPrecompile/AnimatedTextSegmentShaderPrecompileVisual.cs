using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Gallean.Client;

internal sealed class AnimatedTextSegmentShaderPrecompileVisual : AbstractShaderPrecompileVisual
{
    private TextLayout? layout;

    protected override Action<ImmediateDrawingContext> BuildPrecompile()
    {
        Typeface typeface = new(TextElement.GetFontFamily(this));

        layout = new TextLayout("A", typeface, 12.0, foreground: null);

        ShapedTextRun run = layout.TextLines[0].TextRuns.OfType<ShapedTextRun>().First();
        GlyphRun glyphRun = run.GlyphRun;
        Point center = glyphRun.Bounds.Center;
        Matrix transform = Matrix.CreateTranslation((Extent / 2.0) - center.X, (Extent / 2.0) - center.Y);

        AnimatedTextSegmentFrame blurred = new(
            glyphRun,
            Client.Theme.Common.PrimaryTextColor,
            Seed: default,
            transform,
            Opacity: 1.0,
            1.0,
            Dissolve: 0.5,
            Tint: 1.0,
            Glow: 1.0,
            NoiseSize: 12.0);

        AnimatedTextSegmentFrame sharp = blurred with { Blur = 0.0, Glow = 0.0 };

        AnimatedTextRenderOperation operation = new(
            [blurred, sharp],
            smokeFrame: null,
            Client.Theme.Briefing.EmberColor,
            Client.Theme.Briefing.SmokeColor,
            Client.Theme.Common.TextOutlineColor,
            outlineThickness: 1.0,
            1.0,
            TimeSpan.Zero);

        return operation.Render;
    }
}
