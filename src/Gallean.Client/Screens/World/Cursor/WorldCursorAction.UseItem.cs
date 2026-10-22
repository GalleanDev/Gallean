using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal abstract partial record WorldCursorAction
{
    public sealed record UseItem(WorldStackId StackId, WorldItemId ItemId) : WorldCursorAction
    {
        public override WorldStackId? SelectedStackId => StackId;

        public override string SelectionText => Tr("Select a target for the item on the map.");

        public override IWorldCommand<Empty> CreateCommand(WorldPosition position)
        {
            return new WorldUseItemCommand(StackId, ItemId, new WorldItemTarget.Map(new WorldSpellTarget(position, Radius: 0)));
        }

        public override TResult Accept<TContext, TResult>(IWorldCursorActionVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }
}
