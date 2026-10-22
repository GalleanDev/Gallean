namespace Gallean.Client;

internal abstract partial record WorldStackAction(WorldStackId StackId)
{
    public abstract TResult Accept<TResult>(IWorldStackActionVisitor<TResult> visitor);
}
