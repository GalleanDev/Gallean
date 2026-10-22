namespace Gallean.Client;

internal interface IWorldCursorActionVisitor<in TContext, out TResult>
{
    TResult Visit(WorldCursorAction.CastSpell action, TContext context);
    TResult Visit(WorldCursorAction.UseItem action, TContext context);
    TResult Visit(WorldCursorAction.DropItem action, TContext context);
    TResult Visit(WorldCursorAction.CreateRod action, TContext context);
    TResult Visit(WorldCursorAction.DestroyRod action, TContext context);
}
