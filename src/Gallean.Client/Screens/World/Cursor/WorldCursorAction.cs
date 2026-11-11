using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal closed partial record WorldCursorAction
{
    public abstract WorldStackId? SelectedStackId { get; }
    public abstract string SelectionText { get; }
    public virtual ResourceSetViewModel? SelectionCost => null;
    public virtual IReadOnlyList<WorldPosition> TargetPositions => [];

    public abstract IWorldCommand<Empty> CreateCommand(WorldPosition position);

    public abstract TResult Accept<TContext, TResult>(IWorldCursorActionVisitor<TContext, TResult> visitor, TContext context);
}
