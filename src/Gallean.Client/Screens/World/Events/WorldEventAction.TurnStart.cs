using System.Collections.Immutable;

namespace Gallean.Client;

internal abstract partial record WorldEventAction
{
    public sealed record TurnStart(ImmutableArray<AssetSoundKey> Sounds, AutoSave? Save) : WorldEventAction(Sounds)
    {
        public override TResult Accept<TResult>(IWorldEventActionVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
