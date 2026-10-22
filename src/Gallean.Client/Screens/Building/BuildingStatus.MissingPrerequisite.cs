namespace Gallean.Client;

internal abstract partial record BuildingStatus
{
    public sealed record MissingPrerequisite(string PrerequisiteName) : BuildingStatus
    {
        public override TResult Accept<TResult>(IVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
