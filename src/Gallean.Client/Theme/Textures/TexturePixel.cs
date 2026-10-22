using Avalonia.Media;

namespace Gallean.Client;

internal readonly record struct TexturePixel(float Red, float Green, float Blue, float Alpha)
{
    public TexturePixel Over(Color color, float coverage)
    {
        float alpha = coverage + (Alpha * (1 - coverage));
        float kept = Alpha * (1 - coverage);

        if (alpha > 0)
        {
            return new TexturePixel(
                ((color.R * coverage) + (Red * kept)) / alpha,
                ((color.G * coverage) + (Green * kept)) / alpha,
                ((color.B * coverage) + (Blue * kept)) / alpha,
                alpha);
        }
        else
        {
            return this;
        }
    }
}
