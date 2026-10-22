namespace Gallean.Engine.Contracts;

public interface IWorldAiTargetVisitor<in TContext, out TResult>
{
    TResult Visit(WorldAiTarget.Stack target, TContext context);
    TResult Visit(WorldAiTarget.Fortification target, TContext context);
    TResult Visit(WorldAiTarget.Location target, TContext context);
    TResult Visit(WorldAiTarget.Bag target, TContext context);
}
