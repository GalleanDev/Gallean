using System.Collections.Immutable;

namespace Gallean.Client;

internal abstract partial record WorldEventAction
{
    public sealed record StackExhaust(ImmutableArray<AssetSoundKey> Sounds, WorldStackId StackId) : WorldEventAction(Sounds)
    {
        public override TResult Accept<TResult>(IWorldEventActionVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
