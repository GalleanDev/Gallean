using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal abstract partial record WorldCursorAction
{
    public abstract WorldStackId? SelectedStackId { get; }

    public abstract string SelectionText { get; }

    public abstract IWorldCommand<Empty> CreateCommand(WorldPosition position);

    public abstract TResult Accept<TContext, TResult>(IWorldCursorActionVisitor<TContext, TResult> visitor, TContext context);
}
