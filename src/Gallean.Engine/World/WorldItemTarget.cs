namespace Gallean.Engine;

public closed record WorldItemTarget
{
    public sealed record Unit(WorldUnitId UnitId) : WorldItemTarget;

    public sealed record Map(WorldSpellTarget Target) : WorldItemTarget;
}
