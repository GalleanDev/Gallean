namespace Gallean.Client;

internal sealed class GreaterThanConverter : AbstractNumberCompareConverter
{
    public static GreaterThanConverter Instance { get; } = new();

    protected override bool Matches(int value)
    {
        return value > 0;
    }
}
