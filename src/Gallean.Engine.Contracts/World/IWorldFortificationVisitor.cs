namespace Gallean.Engine.Contracts;

public interface IWorldFortificationVisitor<in TContext, out TResult>
{
    TResult Visit(WorldCapital capital, TContext context);
    TResult Visit(WorldCity city, TContext context);
    TResult Visit(WorldRuin ruin, TContext context);
}
