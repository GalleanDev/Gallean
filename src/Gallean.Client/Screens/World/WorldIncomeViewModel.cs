using Gallean.Engine;

namespace Gallean.Client;

internal sealed record WorldIncomeViewModel(WorldGetPlayerInfoCommand.Result Info)
{
    public IReadOnlyList<WorldIncomeRowViewModel> Rows => GetRows();

    private IReadOnlyList<WorldIncomeRowViewModel> GetRows()
    {
        List<WorldIncomeRowViewModel> rows = [];

        foreach (WorldResourceType type in ResourceSetProjection.All.Types)
        {
            int income = GetValue(Info.TotalIncome, type);

            if (income > 0)
            {
                rows.Add(new WorldIncomeRowViewModel(type, GetValue(Info.CapitalIncome, type), GetValue(Info.ResourceNodesIncome, type), income));
            }
        }

        return rows;
    }

    private static int GetValue(WorldResourceSet resources, WorldResourceType type)
    {
        return type switch
        {
            WorldResourceType.Gold => resources.Gold,
            WorldResourceType.LifeMana => resources.LifeMana,
            WorldResourceType.DeathMana => resources.DeathMana,
            WorldResourceType.RuneMana => resources.RuneMana,
            WorldResourceType.InfernalMana => resources.InfernalMana,
            WorldResourceType.GroveMana => resources.GroveMana
        };
    }
}
