namespace Gallean.Engine;

public interface IGameTextProvider
{
    GlobalText GetGlobalText(GlobalTextId id);
    string GetScenarioText(ScenarioTextId id);
}
