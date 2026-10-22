using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetMageInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetMageInfoCommand(
    WorldStackId StackId,
    WorldSiteId SiteId) : IWorldCommand<WorldGetMageInfoCommand.Result>
{
    public sealed record Spell(
        WorldSpellDefinitionId SpellId,
        WorldResourceSet Cost,
        WorldCommandError? Error);

    public sealed record Result(ImmutableArray<Spell> Spells)
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

            return Spells.SequenceEqual(other.Spells);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Spells);

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

        if (site is not WorldMageSite mageSite)
        {
            return CommandResult.Fail(WorldCommandError.SiteNotFound);
        }

        if (stack.Position != mageSite.Entrance)
        {
            return CommandResult.Fail(WorldCommandError.StackNotAtSite);
        }

        ImmutableArray<Spell>.Builder spells = ImmutableArray.CreateBuilder<Spell>(mageSite.Spells.Length);

        foreach (WorldSpellDefinitionId spellId in mageSite.Spells)
        {
            MageSiteCalculator.PurchaseInfo purchase =
                MageSiteCalculator.GetPurchaseInfo(mageSite, stack, player, spellId, context);

            spells.Add(new Spell(spellId, purchase.Cost, purchase.Error));
        }

        return CommandResult.Success(new Result(spells.ToImmutable()), []);
    }
}
