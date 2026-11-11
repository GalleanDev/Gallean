namespace Gallean.Client;

internal closed partial record WorldStackAction
{
    public sealed record DestroyRod(WorldStackId StackId) : WorldStackAction(StackId)
    {
        public override TResult Accept<TResult>(IWorldStackActionVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
