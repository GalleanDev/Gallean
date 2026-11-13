namespace Gallean.Client;

internal static class UnitWardFormatter
{
    public static string FormatName(UnitWard ward)
    {
        return ward switch
        {
            UnitAttackClassWard classWard => UnitAttackFormatter.FormatType(classWard.AttackType),
            UnitAttackSourceWard sourceWard => UnitAttackFormatter.FormatSource(sourceWard.Source)
        };
    }
}
