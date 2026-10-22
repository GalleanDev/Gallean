namespace GameContent;

internal static partial class SpriteOperations
{
    public readonly record struct ContentBox(int MinX, int MinY, int Width, int Height)
    {
        public bool IsEmpty => Width <= 0 || Height <= 0;

        public static ContentBox Empty => new(0, 0, 0, 0);

        public ContentBox Union(ContentBox other)
        {
            if (IsEmpty)
            {
                return other;
            }
            else if (other.IsEmpty)
            {
                return this;
            }
            else
            {
                int minX = Math.Min(MinX, other.MinX);
                int minY = Math.Min(MinY, other.MinY);
                int maxX = Math.Max(MinX + Width, other.MinX + other.Width) - 1;
                int maxY = Math.Max(MinY + Height, other.MinY + other.Height) - 1;

                return new ContentBox(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }
        }
    }
}
