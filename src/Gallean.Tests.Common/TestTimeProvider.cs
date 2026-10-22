namespace Gallean.Tests.Common;

public sealed class TestTimeProvider(DateTimeOffset now, TimeZoneInfo localTimeZone) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => localTimeZone;

    public override DateTimeOffset GetUtcNow()
    {
        return now;
    }
}
