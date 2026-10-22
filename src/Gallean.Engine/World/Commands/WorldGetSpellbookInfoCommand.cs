using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetSpellbookInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetSpellbookInfoCommand(WorldPlayerId PlayerId) : IWorldCommand<WorldGetSpellbookInfoCommand.Result>
{
    public sealed record Cast(
        WorldSpellDefinitionId SpellId,
        WorldResourceSet Cost,
        WorldCommandError? Error);

    public sealed record Research(
        WorldSpellDefinitionId SpellId,
        WorldResourceSet Cost,
        WorldCommandError? Error);

    public sealed record Result(
        ImmutableArray<Cast> Acquired,
        ImmutableArray<Research> Available,
        ImmutableArray<WorldSpellDefinitionId> Locked,
        WorldPlayerBuildingDefinitionId? RequiredBuildingId,
        WorldCommandError? RequiredBuildingError)
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

            return Acquired.SequenceEqual(other.Acquired) &&
                   Available.SequenceEqual(other.Available) &&
                   Locked.SequenceEqual(other.Locked) &&
                   RequiredBuildingId == other.RequiredBuildingId &&
                   RequiredBuildingError == other.RequiredBuildingError;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Acquired);
            hash.AddSequence(Available);
            hash.AddSequence(Locked);
            hash.Add(RequiredBuildingId);
            hash.Add(RequiredBuildingError);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        if (PlayerId != state.TurnInfo.CurrentPlayerId)
        {
            return CommandResult.Fail(WorldCommandError.NotPlayerTurn);
        }

        WorldPlayer player = state.CurrentPlayer;

        LordDefinition lord = context.Definitions.GetLord(player.LordId);
        IReadOnlyDictionary<WorldSpellDefinitionId, WorldResourceSet> researchCosts =
            context.Definitions.GetSpellResearchCosts(player.Race);

        ImmutableArray<Cast>.Builder acquired = ImmutableArray.CreateBuilder<Cast>();

        foreach (WorldSpellDefinitionId spellId in player.Spellbook.Acquired)
        {
            SpellCalculator.CastInfo castInfo = SpellCalculator.GetSpellCastInfo(spellId, player, context);

            acquired.Add(new Cast(spellId, castInfo.Cost, castInfo.Error));
        }

        ImmutableArray<Research>.Builder available = ImmutableArray.CreateBuilder<Research>();

        foreach (WorldSpellDefinitionId spellId in player.Spellbook.Available)
        {
            if (!player.Spellbook.Acquired.Contains(spellId))
            {
                WorldResourceSet listPrice = researchCosts.GetValueOrDefault(spellId);
                WorldResourceSet cost = SpellCalculator.GetResearchCost(listPrice, lord.Category, context.Globals);
                WorldCommandResult researchResult = new WorldResearchSpellCommand(PlayerId, spellId).Execute(state, context);

                available.Add(new Research(spellId, cost, researchResult.Error));
            }
        }

        ImmutableArray<WorldSpellDefinitionId>.Builder locked = ImmutableArray.CreateBuilder<WorldSpellDefinitionId>();

        foreach (WorldSpellDefinitionId spellId in researchCosts.Keys)
        {
            if (!player.Spellbook.Available.Contains(spellId) && !player.Spellbook.Acquired.Contains(spellId))
            {
                locked.Add(spellId);
            }
        }

        locked.Sort();

        WorldPlayerBuildingDefinitionId? requiredBuildingId = FindRequiredBuilding(player, context);
        WorldCommandError? requiredBuildingError;

        if (requiredBuildingId is not null)
        {
            WorldConstructBuildingCommand constructCommand = new(PlayerId, requiredBuildingId.Value);
            WorldCommandResult constructResult = constructCommand.Execute(state, context);

            requiredBuildingError = constructResult.Error;
        }
        else
        {
            requiredBuildingError = null;
        }

        return CommandResult.Success(
            new Result(acquired.ToImmutable(), available.ToImmutable(), locked.ToImmutable(), requiredBuildingId, requiredBuildingError),
            []);
    }

    private static WorldPlayerBuildingDefinitionId? FindRequiredBuilding(WorldPlayer player, IEngineContext context)
    {
        if (!PlayerBuildingsCalculator.IsAcquired(player.Buildings, WorldPlayerBuildingCategory.Spell, context.Definitions))
        {
            return PlayerBuildingsCalculator.FindAvailable(player.Buildings, WorldPlayerBuildingCategory.Spell, context.Definitions);
        }
        else
        {
            return null;
        }
    }
}
