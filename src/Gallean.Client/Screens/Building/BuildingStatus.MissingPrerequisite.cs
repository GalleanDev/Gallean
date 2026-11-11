namespace Gallean.Client;

internal closed partial record BuildingStatus
{
    public sealed record MissingPrerequisite(string PrerequisiteName) : BuildingStatus
    {
        public override TResult Accept<TResult>(IVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
