using Gallean.Common;

namespace Gallean.Engine;

public closed record ScenarioEventStackOwner
{
    public sealed record Subrace(WorldSubraceId Id) : ScenarioEventStackOwner;

    public sealed record Triggerer : ScenarioEventStackOwner, IStaticInstance<Triggerer>;
}
