using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal closed partial record WorldCursorAction
{
    public sealed record CreateRod(WorldStackId StackId, WorldResourceSet Cost, IReadOnlyList<WorldPosition> Positions) : WorldCursorAction
    {
        public override WorldStackId? SelectedStackId => StackId;
        public override string SelectionText => Tr("Select a tile to plant a rod on.");
        public override ResourceSetViewModel SelectionCost => new(Cost, ResourceSetProjection.All);
        public override IReadOnlyList<WorldPosition> TargetPositions => Positions;

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
