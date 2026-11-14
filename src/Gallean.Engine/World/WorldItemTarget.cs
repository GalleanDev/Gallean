namespace Gallean.Engine;

public closed record WorldItemTarget
{
    public sealed record Unit(WorldUnitGroup Group, WorldUnitId UnitId) : WorldItemTarget;

    public sealed record Map(WorldSpellTarget Target) : WorldItemTarget;
}
