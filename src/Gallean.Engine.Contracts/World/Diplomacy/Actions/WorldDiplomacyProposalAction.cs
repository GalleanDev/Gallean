using System.Text.Json.Serialization;

namespace Gallean.Engine.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(WorldDiplomacyTradeItemAction), "tradeItem")]
[JsonDerivedType(typeof(WorldDiplomacyTradeSpellAction), "tradeSpell")]
[JsonDerivedType(typeof(WorldDiplomacyProposeAllianceAction), "proposeAlliance")]
[JsonDerivedType(typeof(WorldDiplomacyExchangeMapAction), "exchangeMap")]
public abstract record WorldDiplomacyProposalAction : WorldDiplomacyAction
{
    public abstract TResult AcceptProposal<TContext, TResult>(
        IWorldDiplomacyProposalActionVisitor<TContext, TResult> visitor,
        TContext context);
}
