using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal abstract partial record WorldCursorAction
{
    public sealed record CastSpell(WorldSpellDefinitionId SpellId) : WorldCursorAction
    {
        public override WorldStackId? SelectedStackId => null;

        public override string SelectionText => Tr("Select a target for the spell on the map.");

        public override IWorldCommand<Empty> CreateCommand(WorldPosition position)
        {
            return new WorldCastSpellCommand(SpellId, new WorldSpellTarget(position, Radius: 0));
        }

        public override TResult Accept<TContext, TResult>(IWorldCursorActionVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }
}
