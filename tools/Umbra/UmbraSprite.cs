namespace Umbra;

public sealed record UmbraSprite(UmbraSpriteMetadata Metadata, ReadOnlyMemory<byte> Pixels);
