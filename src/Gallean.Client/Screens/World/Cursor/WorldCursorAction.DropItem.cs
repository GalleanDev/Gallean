using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal abstract partial record WorldCursorAction
{
    public sealed record DropItem(WorldStackId StackId, WorldItemId ItemId) : WorldCursorAction
    {
        public override WorldStackId? SelectedStackId => StackId;

        public override string SelectionText => Tr("Select a tile to drop the item on.");

        public override IWorldCommand<Empty> CreateCommand(WorldPosition position)
        {
            return new WorldDropItemCommand(StackId, position, ItemId);
        }

        public override TResult Accept<TContext, TResult>(IWorldCursorActionVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }
}
