using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetDiplomacyInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetDiplomacyInfoCommand : IWorldCommand<WorldGetDiplomacyInfoCommand.Result>
{
    public sealed record TradeItem(
        WorldFortificationId SourceFortificationId,
        ItemDefinitionId ItemId,
        int Value,
        WorldCommandError? Error);

    public sealed record TradeSpell(
        WorldSpellDefinitionId SpellId,
        int Value,
        WorldCommandError? Error);

    public sealed record Player(
        WorldPlayerId PlayerId,
        WorldRelation Relation,
        WorldRelationLevel Level,
        WorldCommandError? GiveGoldError,
        WorldCommandError? ProposeAllianceError,
        WorldCommandError? BreakAllianceError,
        WorldCommandError? ExchangeMapError,
        ImmutableArray<TradeItem> Items,
        ImmutableArray<TradeSpell> Spells)
    {
        public bool Equals(Player? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return PlayerId == other.PlayerId &&
                   Relation == other.Relation &&
                   Level == other.Level &&
                   GiveGoldError == other.GiveGoldError &&
                   ProposeAllianceError == other.ProposeAllianceError &&
                   BreakAllianceError == other.BreakAllianceError &&
                   ExchangeMapError == other.ExchangeMapError &&
                   Items.SequenceEqual(other.Items) &&
                   Spells.SequenceEqual(other.Spells);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(PlayerId);
            hash.Add(Relation);
            hash.Add(Level);
            hash.Add(GiveGoldError);
            hash.Add(ProposeAllianceError);
            hash.Add(BreakAllianceError);
            hash.Add(ExchangeMapError);
            hash.AddSequence(Items);
            hash.AddSequence(Spells);

            return hash.ToHashCode();
        }
    }

    public sealed record Result(ImmutableArray<Player> Players)
    {
        public bool Equals(Result? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Players.SequenceEqual(other.Players);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Players);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.Players[state.TurnInfo.CurrentPlayerId];
        WorldPlan plan = WorldPlan.For(state);
        ImmutableArray<Player>.Builder players = ImmutableArray.CreateBuilder<Player>();

        foreach (WorldPlayer targetPlayer in state.Players.Values)
        {
            if (targetPlayer.Id == player.Id || targetPlayer.Race == WorldRace.Neutral || plan.IsPlayerDefeated(targetPlayer.Id))
            {
                continue;
            }

            WorldRelation relation = DiplomacyCalculator.GetRelation(state, player.Id, targetPlayer.Id);

            WorldCommandResult giveGoldResult = new WorldDiplomacyActionCommand(
                targetPlayer.Id,
                new WorldDiplomacyGiveGoldAction(context.Globals.DiplomacyGiftGoldPerRelationChange)).Execute(state, context);
            WorldCommandResult proposeAllianceResult = new WorldDiplomacyActionCommand(
                targetPlayer.Id,
                new WorldDiplomacyProposeAllianceAction()).Execute(state, context);
            WorldCommandResult breakAllianceResult = new WorldDiplomacyActionCommand(
                targetPlayer.Id,
                new WorldDiplomacyBreakAllianceAction()).Execute(state, context);
            WorldCommandResult exchangeMapResult = new WorldDiplomacyActionCommand(
                targetPlayer.Id,
                new WorldDiplomacyExchangeMapAction()).Execute(state, context);

            players.Add(new Player(
                targetPlayer.Id,
                relation,
                DiplomacyCalculator.ResolveRelationLevel(relation.Current, context.Globals),
                giveGoldResult.Error,
                proposeAllianceResult.Error,
                breakAllianceResult.Error,
                exchangeMapResult.Error,
                BuildItems(state, context, player, targetPlayer.Id),
                BuildSpells(state, context, player, targetPlayer.Id)));
        }

        return CommandResult.Success(new Result(players.ToImmutable()), []);
    }

    private static ImmutableArray<TradeItem> BuildItems(
        WorldState state,
        IEngineContext context,
        WorldPlayer player,
        WorldPlayerId targetPlayerId)
    {
        ImmutableArray<TradeItem>.Builder items = ImmutableArray.CreateBuilder<TradeItem>();

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (FortificationCalculator.GetOwner(fortification) != player.Id)
            {
                continue;
            }

            HashSet<ItemDefinitionId> offered = [];

            foreach (WorldItem item in fortification.Inventory.Items)
            {
                if (!offered.Add(item.DefinitionId))
                {
                    continue;
                }

                WorldDiplomacyProposal proposal = new(player.Id, targetPlayerId, new WorldDiplomacyTradeItemAction(fortification.Id, item.DefinitionId, Price: 0));
                DiplomacyProposalCalculator.SendInfo info = DiplomacyProposalCalculator.GetSendInfo(proposal, state, context);

                items.Add(new TradeItem(fortification.Id, item.DefinitionId, info.TradeValue, info.Error));
            }
        }

        return items.ToImmutable();
    }

    private static ImmutableArray<TradeSpell> BuildSpells(
        WorldState state,
        IEngineContext context,
        WorldPlayer player,
        WorldPlayerId targetPlayerId)
    {
        ImmutableArray<TradeSpell>.Builder spells = ImmutableArray.CreateBuilder<TradeSpell>();

        foreach (WorldSpellDefinitionId spellId in player.Spellbook.Acquired)
        {
            WorldDiplomacyProposal proposal = new(player.Id, targetPlayerId, new WorldDiplomacyTradeSpellAction(spellId, Price: 0));
            DiplomacyProposalCalculator.SendInfo info = DiplomacyProposalCalculator.GetSendInfo(proposal, state, context);

            spells.Add(new TradeSpell(spellId, info.TradeValue, info.Error));
        }

        return spells.ToImmutable();
    }
}
