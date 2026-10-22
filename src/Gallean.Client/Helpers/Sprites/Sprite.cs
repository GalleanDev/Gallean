using SkiaSharp;

namespace Gallean.Client;

internal sealed record Sprite(
    AssetSpriteMetadata Metadata,
    SKImage Image,
    SKImage? Normal,
    SKImage? Depth,
    SKImage? Albedo,
    IReadOnlyList<SpriteLight> Lights);
