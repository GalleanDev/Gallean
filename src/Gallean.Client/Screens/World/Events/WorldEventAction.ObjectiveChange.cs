using System.Collections.Immutable;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public sealed record ObjectiveChange(ImmutableArray<AssetSoundKey> Sounds, ScenarioTextId TextId) : WorldEventAction(Sounds)
    {
        public override TResult Accept<TResult>(IWorldEventActionVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
