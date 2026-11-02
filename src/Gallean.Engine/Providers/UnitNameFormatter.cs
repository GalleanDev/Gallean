namespace Gallean.Engine;

internal sealed class UnitNameFormatter(IGameTextProvider texts) : IUnitNameVisitor<string>
{
    public string Visit(LiteralUnitName name)
    {
        return name.Text;
    }

    public string Visit(ScenarioUnitName name)
    {
        return texts.GetScenarioText(name.Id);
    }

    public string Visit(LeaderUnitName name)
    {
        return texts.GetLeaderName(name.Id);
    }
}
