namespace Gallean.Engine;

internal static class ResourceNodeCalculator
{
    public static WorldResourceSet GetIncome(WorldResourceNode node, GlobalVariables globals)
    {
        int income = globals.ResourceNodeIncome;

        return node.ResourceType switch
        {
            WorldResourceType.Gold => new WorldResourceSet(income, 0, 0, 0, 0, 0),
            WorldResourceType.DeathMana => new WorldResourceSet(0, income, 0, 0, 0, 0),
            WorldResourceType.RuneMana => new WorldResourceSet(0, 0, income, 0, 0, 0),
            WorldResourceType.LifeMana => new WorldResourceSet(0, 0, 0, income, 0, 0),
            WorldResourceType.InfernalMana => new WorldResourceSet(0, 0, 0, 0, income, 0),
            WorldResourceType.GroveMana => new WorldResourceSet(0, 0, 0, 0, 0, income)
        };
    }
}
