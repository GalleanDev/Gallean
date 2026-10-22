using System.Collections.Immutable;

namespace Gallean.Client;

internal abstract partial record WorldEventAction
{
    public sealed record ThiefAction(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        WorldStackId StackId,
        WorldPosition TargetPosition) : Screen(Sounds, FocusPosition)
    {
        public override TResult Accept<TResult>(IWorldEventScreenVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
