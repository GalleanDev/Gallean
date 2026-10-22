using System.Collections.Immutable;
using Gallean.Configuration;

namespace Gallean.Client;

internal abstract partial record WorldEventAction
{
    public sealed record AutoSave(
        ImmutableArray<AssetSoundKey> Sounds,
        GameSaveType Type,
        string Name) : WorldEventAction(Sounds)
    {
        public override TResult Accept<TResult>(IWorldEventActionVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
