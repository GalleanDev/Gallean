using Gallean.Common;

namespace Gallean.Engine.Tests;

internal sealed class TestGameTextProvider : IGameTextProvider, IStaticInstance<TestGameTextProvider>
{
    public GlobalText GetGlobalText(GlobalTextId id)
    {
        return new GlobalText(id, id.Value, IsVerified: true, Context: null);
    }

    public string GetScenarioText(ScenarioTextId id)
    {
        return id.Value;
    }
}
