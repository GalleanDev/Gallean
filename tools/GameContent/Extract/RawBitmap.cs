namespace GameContent;

public sealed record RawBitmap
{
    public int OriginalWidth { get; init; }
    public int OriginalHeight { get; init; }
    public Rectangle Bounds { get; init; }
    public bool Additive { get; init; }
    public required byte[] Data { get; init; }
}
