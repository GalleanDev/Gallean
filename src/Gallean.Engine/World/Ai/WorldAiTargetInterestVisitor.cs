namespace Gallean.Engine;

internal sealed class WorldAiTargetInterestVisitor(WorldAiAttitudeDefinition attitude)
    : IWorldAiTargetVisitor<WorldAiTargetInterestVisitor.Context, int>
{
    internal readonly record struct Context(WorldState State, WorldPlayerId PlayerId);

    public int Visit(WorldAiTarget.Stack target, Context context)
    {
        WorldStack stack = context.State.Stacks[target.StackId];

        return MapInterest(
            context,
            stack.OwnerId,
            attitude.NeutralStackInterest,
            attitude.AiPlayerStackInterest,
            attitude.HumanPlayerStackInterest);
    }

    public int Visit(WorldAiTarget.Fortification target, Context context)
    {
        WorldFortification fortification = context.State.Fortifications[target.FortificationId];

        WorldPlayerId? ownerId = FortificationCalculator.GetOwner(fortification);

        if (ownerId is not null)
        {
            return MapInterest(
                context,
                ownerId.Value,
                attitude.NeutralCityInterest,
                attitude.AiPlayerCityInterest,
                attitude.HumanPlayerCityInterest);
        }
        else
        {
            return attitude.RuinInterest;
        }
    }

    public int Visit(WorldAiTarget.Location target, Context context)
    {
        throw new InvalidOperationException("Invalid world AI target type.");
    }

    public int Visit(WorldAiTarget.Bag target, Context context)
    {
        return attitude.BagInterest;
    }

    private static int MapInterest(Context context, WorldPlayerId ownerId, int neutralInterest, int aiPlayerInterest, int humanPlayerInterest)
    {
        WorldPlayer owner = context.State.Players[ownerId];

        if (owner.Race == WorldRace.Neutral)
        {
            return neutralInterest;
        }
        else if (owner.IsAi)
        {
            return aiPlayerInterest;
        }
        else
        {
            return humanPlayerInterest;
        }
    }
}
