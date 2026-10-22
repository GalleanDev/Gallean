using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetMercenaryInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetMercenaryInfoCommand(
    WorldStackId StackId,
    WorldSiteId SiteId) : IWorldCommand<WorldGetMercenaryInfoCommand.Result>
{
    public sealed record Mercenary(
        UnitDefinitionId DefinitionId,
        int Level,
        WorldResourceSet Cost,
        WorldCommandError? Error);

    public sealed record Result(ImmutableArray<Mercenary> Mercenaries)
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

            return Mercenaries.SequenceEqual(other.Mercenaries);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Mercenaries);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.CurrentPlayer;

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return CommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!state.Sites.TryGetValue(SiteId, out WorldSite? site))
        {
            return CommandResult.Fail(WorldCommandError.SiteNotFound);
        }

        if (site is not WorldMercenarySite mercenarySite)
        {
            return CommandResult.Fail(WorldCommandError.SiteNotFound);
        }

        if (stack.Position != mercenarySite.Entrance)
        {
            return CommandResult.Fail(WorldCommandError.StackNotAtSite);
        }

        ImmutableArray<Mercenary>.Builder mercenaries = ImmutableArray.CreateBuilder<Mercenary>(mercenarySite.Units.Length);

        foreach (WorldMercenaryUnit mercenaryUnit in mercenarySite.Units)
        {
            MercenarySiteCalculator.PurchaseInfo purchase = MercenarySiteCalculator.GetPurchaseInfo(
                mercenarySite,
                state,
                stack,
                player,
                mercenaryUnit.DefinitionId,
                mercenaryUnit.Level,
                context);

            mercenaries.Add(new Mercenary(mercenaryUnit.DefinitionId, mercenaryUnit.Level, purchase.Cost, purchase.Error));
        }

        return CommandResult.Success(new Result(mercenaries.ToImmutable()), []);
    }
}
