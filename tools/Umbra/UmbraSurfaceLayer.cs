namespace Umbra;

public sealed record UmbraSurfaceLayer(UmbraSpriteHash Hash, UmbraSurfaceLayerType Type, int PixelWidth, int PixelHeight, ReadOnlyMemory<byte> Pixels);
