namespace Gallean.Entities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ScenarioDataFileAttribute(string fileName) : Attribute
{
    public string FileName { get; } = fileName;
    public bool PerScenario { get; init; }
    public bool Index { get; init; }
}
