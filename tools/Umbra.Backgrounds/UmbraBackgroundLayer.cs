namespace Umbra.Backgrounds;

public sealed record UmbraBackgroundLayer(int PixelWidth, int PixelHeight, ReadOnlyMemory<byte> Data);
