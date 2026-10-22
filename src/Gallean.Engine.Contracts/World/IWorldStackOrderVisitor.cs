namespace Gallean.Engine.Contracts;

public interface IWorldStackOrderVisitor<in TContext, out TResult>
{
    TResult Visit(WorldStackOrder.Normal order, TContext context);
    TResult Visit(WorldStackOrder.Stand order, TContext context);
    TResult Visit(WorldStackOrder.Guard order, TContext context);
    TResult Visit(WorldStackOrder.AttackStack order, TContext context);
    TResult Visit(WorldStackOrder.DefendStack order, TContext context);
    TResult Visit(WorldStackOrder.SecureCity order, TContext context);
    TResult Visit(WorldStackOrder.Roam order, TContext context);
    TResult Visit(WorldStackOrder.Explore order, TContext context);
    TResult Visit(WorldStackOrder.MoveToLocation order, TContext context);
    TResult Visit(WorldStackOrder.DefendLocation order, TContext context);
    TResult Visit(WorldStackOrder.Berserk order, TContext context);
    TResult Visit(WorldStackOrder.Assist order, TContext context);
    TResult Visit(WorldStackOrder.Steal order, TContext context);
    TResult Visit(WorldStackOrder.DefendCity order, TContext context);
    TResult Visit(WorldStackOrder.Heal order, TContext context);

    TResult Visit(WorldStackOrder.Reinforce order, TContext context);
}
