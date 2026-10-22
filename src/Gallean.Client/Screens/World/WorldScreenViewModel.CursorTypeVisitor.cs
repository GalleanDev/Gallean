using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class WorldScreenViewModel
{
    private sealed class CursorTypeVisitor(GameModel gameModel) : IWorldCursorActionVisitor<WorldPosition, AssetAnimationKey.CursorType>
    {
        public AssetAnimationKey.CursorType Visit(WorldCursorAction.CastSpell action, WorldPosition position)
        {
            return ResolveActionCursor(
                action.CreateCommand(position),
                AssetAnimationKey.CursorType.CastSpell,
                AssetAnimationKey.CursorType.CastSpellInvalid);
        }

        public AssetAnimationKey.CursorType Visit(WorldCursorAction.UseItem action, WorldPosition position)
        {
            return ResolveActionCursor(
                action.CreateCommand(position),
                AssetAnimationKey.CursorType.UseScroll,
                AssetAnimationKey.CursorType.UseScrollInvalid);
        }

        public AssetAnimationKey.CursorType Visit(WorldCursorAction.DropItem action, WorldPosition position)
        {
            return ResolveActionCursor(
                action.CreateCommand(position),
                AssetAnimationKey.CursorType.DropBag,
                AssetAnimationKey.CursorType.Default);
        }

        public AssetAnimationKey.CursorType Visit(WorldCursorAction.CreateRod action, WorldPosition position)
        {
            return ResolveActionCursor(
                action.CreateCommand(position),
                AssetAnimationKey.CursorType.CreateRod,
                AssetAnimationKey.CursorType.CreateRodInvalid);
        }

        public AssetAnimationKey.CursorType Visit(WorldCursorAction.DestroyRod action, WorldPosition position)
        {
            return ResolveActionCursor(
                action.CreateCommand(position),
                AssetAnimationKey.CursorType.DestroyRod,
                AssetAnimationKey.CursorType.Default);
        }

        private AssetAnimationKey.CursorType ResolveActionCursor<TResult>(
            IWorldCommand<TResult> command,
            AssetAnimationKey.CursorType valid,
            AssetAnimationKey.CursorType invalid)
        {
            ICommandResult<WorldCommandError, WorldEvent, TResult> result = gameModel.World.DryRunCommand(command);

            if (result.Error is null)
            {
                return valid;
            }
            else
            {
                return invalid;
            }
        }
    }
}
