using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal abstract partial record WorldCursorAction
{
    public sealed record CreateRod(WorldStackId StackId) : WorldCursorAction
    {
        public override WorldStackId? SelectedStackId => StackId;

        public override string SelectionText => Tr("Select a tile to plant a rod on.");

        public override IWorldCommand<Empty> CreateCommand(WorldPosition position)
        {
            return new WorldCreateRodCommand(StackId, position);
        }

        public override TResult Accept<TContext, TResult>(IWorldCursorActionVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }
}
