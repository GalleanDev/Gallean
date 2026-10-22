namespace Gallean.Client;

internal interface IWorldStackActionVisitor<out TResult>
{
    TResult Visit(WorldStackAction.Exchange action);
    TResult Visit(WorldStackAction.LearnAbility action);
    TResult Visit(WorldStackAction.CreateRod action);
    TResult Visit(WorldStackAction.DestroyRod action);
    TResult Visit(WorldStackAction.UseItem action);
}
