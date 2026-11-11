namespace Gallean.Client;

internal closed partial record WorldStackAction(WorldStackId StackId)
{
    public abstract TResult Accept<TResult>(IWorldStackActionVisitor<TResult> visitor);
}
