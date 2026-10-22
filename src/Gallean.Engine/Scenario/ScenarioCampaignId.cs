namespace Gallean.Engine;

public readonly record struct ScenarioCampaignId(string Value) : IComparable<ScenarioCampaignId>, ISerializableString
{
    public int CompareTo(ScenarioCampaignId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(ScenarioCampaignId other)
    {
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public override string ToString()
    {
        return Value;
    }
}
