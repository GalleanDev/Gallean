namespace Gallean.Engine;

public interface IWorldPathEncounterVisitor<in TContext, out TResult>
{
    TResult Visit(WorldPathEncounter.Battle encounter, TContext context);
    TResult Visit(WorldPathEncounter.Thief encounter, TContext context);
    TResult Visit(WorldPathEncounter.Bag encounter, TContext context);
    TResult Visit(WorldPathEncounter.Site encounter, TContext context);
}
