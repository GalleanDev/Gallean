namespace Gallean.Client;

internal abstract partial record WorldStackAction
{
    public sealed record LearnAbility(WorldStackId StackId) : WorldStackAction(StackId)
    {
        public override TResult Accept<TResult>(IWorldStackActionVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
