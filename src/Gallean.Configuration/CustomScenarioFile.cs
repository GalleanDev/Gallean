namespace Gallean.Configuration;

public sealed record CustomScenarioFile(CustomScenarioId Id, CustomScenarioType Type, byte[] Data)
{
    public static CustomScenarioFile Create(CustomScenarioType type, byte[] data)
    {
        return new CustomScenarioFile(CustomScenarioId.FromContent(data), type, data);
    }
}
