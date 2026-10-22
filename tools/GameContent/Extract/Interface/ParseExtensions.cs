namespace GameContent;

internal static class ParseExtensions
{
    extension(IReadOnlyList<string> data)
    {
        public Rectangle ParseBounds(int startIndex, int offsetX = 0, int offsetY = 0)
        {
            return Rectangle.FromLtrb(
                int.Parse(data[startIndex]) + offsetX,
                int.Parse(data[startIndex + 1]) + offsetY,
                int.Parse(data[startIndex + 2]) + offsetX,
                int.Parse(data[startIndex + 3]) + offsetY);
        }

        public Point ParsePoint(int startIndex)
        {
            return new Point(
                int.Parse(data[startIndex]),
                int.Parse(data[startIndex + 1]));
        }
    }

    extension(string data)
    {
        public string? ParseImageName()
        {
            return string.IsNullOrEmpty(data) ? null : data;
        }

        public string? ParseObjectName()
        {
            return string.IsNullOrEmpty(data) ? null : data;
        }

        public string? ParseTextId()
        {
            string trimmed = data.Trim('\"');
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        public string? ParseTextStyle()
        {
            string trimmed = data.Trim('\"');
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        public int ParseInt()
        {
            return int.Parse(data);
        }

        public bool ParseBoolean()
        {
            return int.Parse(data) == 1;
        }
    }
}
