using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Client;

internal abstract partial record WorldEventAction
{
    public sealed record SpellsReceive(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        ImmutableArray<WorldSpellDefinitionId> Spells) : Screen(Sounds, FocusPosition)
    {
        public bool Equals(SpellsReceive? other)
        {
            return base.Equals(other) &&
                   Spells.SequenceEqual(other.Spells);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(base.GetHashCode());
            hash.AddSequence(Spells);

            return hash.ToHashCode();
        }

        public override TResult Accept<TResult>(IWorldEventScreenVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
