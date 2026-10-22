using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal static partial class ThiefCalculator
{
    private sealed class StealableSpellsVisitor :
        IThiefTargetVisitor<WorldState, ImmutableArray<WorldSpellDefinitionId>>,
        IStaticInstance<StealableSpellsVisitor>
    {
        public ImmutableArray<WorldSpellDefinitionId> Visit(ThiefStackTarget target, WorldState state)
        {
            return [];
        }

        public ImmutableArray<WorldSpellDefinitionId> Visit(ThiefFortificationTarget target, WorldState state)
        {
            WorldPlayer owner = state.Players[target.Owner];

            return [.. owner.Spellbook.Acquired];
        }

        public ImmutableArray<WorldSpellDefinitionId> Visit(ThiefMerchantSiteTarget target, WorldState state)
        {
            return [];
        }

        public ImmutableArray<WorldSpellDefinitionId> Visit(ThiefMageSiteTarget target, WorldState state)
        {
            return target.Site.Spells;
        }

        public ImmutableArray<WorldSpellDefinitionId> Visit(ThiefRuinTarget target, WorldState state)
        {
            return [];
        }
    }
}
