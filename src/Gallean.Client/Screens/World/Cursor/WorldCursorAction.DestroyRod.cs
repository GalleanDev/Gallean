using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal closed partial record WorldCursorAction
{
    public sealed record DestroyRod(WorldStackId StackId) : WorldCursorAction
    {
        public override WorldStackId? SelectedStackId => StackId;

        public override string SelectionText => Tr("Select a rod to destroy.");

        public override IWorldCommand<Empty> CreateCommand(WorldPosition position)
        {
            return new WorldRemoveRodCommand(StackId, position);
        }
    }
}
