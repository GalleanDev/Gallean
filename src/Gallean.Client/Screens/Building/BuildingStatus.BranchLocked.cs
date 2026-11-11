namespace Gallean.Client;

internal closed partial record BuildingStatus
{
    public sealed record BranchLocked : BuildingStatus
    {
        public override TResult Accept<TResult>(IVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
