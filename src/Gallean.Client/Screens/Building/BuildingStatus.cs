namespace Gallean.Client;

internal abstract partial record BuildingStatus
{
    public interface IVisitor<out TResult>
    {
        TResult Visit(Constructible status);
        TResult Visit(Built status);
        TResult Visit(BranchLocked status);
        TResult Visit(MissingPrerequisite status);
        TResult Visit(ScenarioLocked status);
        TResult Visit(Unavailable status);
    }

    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);
}
