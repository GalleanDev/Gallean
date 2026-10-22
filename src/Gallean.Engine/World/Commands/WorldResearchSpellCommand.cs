using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldResearchSpellCommand(
    WorldPlayerId PlayerId,
    WorldSpellDefinitionId SpellId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (PlayerId != state.TurnInfo.CurrentPlayerId)
        {
            return WorldCommandResult.Fail(WorldCommandError.NotPlayerTurn);
        }

        WorldPlayer player = state.CurrentPlayer;

        if (player.TurnActions.SpellResearched)
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellAlreadyResearchedThisTurn);
        }

        if (player.Spellbook.Acquired.Contains(SpellId))
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellAlreadyKnown);
        }

        if (!PlayerBuildingsCalculator.IsAcquired(player.Buildings, WorldPlayerBuildingCategory.Spell, context.Definitions))
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellBuildingRequired);
        }

        if (!player.Spellbook.Available.Contains(SpellId))
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellNotResearchable);
        }

        LordDefinition lord = context.Definitions.GetLord(player.LordId);
        WorldSpellDefinition spell = context.Definitions.GetSpell(SpellId);

        if (spell.Level > GetSpellResearchLevelLimit(lord.Category, context.Globals))
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellResearchLevelTooHigh);
        }

        IReadOnlyDictionary<WorldSpellDefinitionId, WorldResourceSet> researchCosts =
            context.Definitions.GetSpellResearchCosts(player.Race);

        if (!researchCosts.TryGetValue(SpellId, out WorldResourceSet researchCost))
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellNotResearchable);
        }

        WorldResourceSet chargedCost =
            SpellCalculator.GetResearchCost(researchCost, lord.Category, context.Globals);

        if (!ResourceSetCalculator.CanAfford(player.Resources, chargedCost))
        {
            return WorldCommandResult.Fail(WorldCommandError.InsufficientResources);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldSpellResearchedEvent(PlayerId, SpellId, chargedCost)]);
    }

    private static int GetSpellResearchLevelLimit(LordCategory category, GlobalVariables globals)
    {
        return category switch
        {
            LordCategory.Warrior => globals.LordWarriorSpellResearchLevelLimit,
            LordCategory.Mage => globals.LordMageSpellResearchLevelLimit,
            LordCategory.Guildmaster => globals.LordGuildmasterSpellResearchLevelLimit
        };
    }
}
