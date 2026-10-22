namespace Gallean.Client;

internal sealed class LessThanConverter : AbstractNumberCompareConverter
{
    public static LessThanConverter Instance { get; } = new();

    protected override bool Matches(int value)
    {
        return value < 0;
    }
}
