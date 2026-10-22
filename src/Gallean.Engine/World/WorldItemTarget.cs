namespace Gallean.Engine;

public abstract record WorldItemTarget
{
    public sealed record Unit(WorldUnitId UnitId) : WorldItemTarget;

    public sealed record Map(WorldSpellTarget Target) : WorldItemTarget;
}
