namespace Gallean.Engine;

public abstract record WorldUnitGroup
{
    public sealed record Stack(WorldStackId StackId) : WorldUnitGroup;

    public sealed record Fortification(WorldFortificationId FortificationId) : WorldUnitGroup;
}
