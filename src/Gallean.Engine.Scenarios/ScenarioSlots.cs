namespace Gallean.Engine.Scenarios;

public static class ScenarioSlots
{
    public const int CampaignCustom = 900;

    public static Range Default { get; } = 200..900;
    public static Range Custom { get; } = 900..1000;
    public static Range CampaignDefault { get; } = 200..900;

    public static Db.ScenarioId GetScenarioId(int slot)
    {
        return new Db.ScenarioId($"S{slot:D3}SC0000");
    }

    public static Db.Id GetCampaignId(int slot)
    {
        return new Db.Id($"C000CC{slot:D4}");
    }
}
