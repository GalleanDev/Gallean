using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Client;

internal static class UnitWardFormatter
{
    private sealed class NameVisitor : IUnitWardVisitor<Empty, string>, IStaticInstance<NameVisitor>
    {
        public string Visit(UnitAttackClassWard ward, Empty context)
        {
            return UnitAttackFormatter.FormatType(ward.AttackType);
        }

        public string Visit(UnitAttackSourceWard ward, Empty context)
        {
            return UnitAttackFormatter.FormatSource(ward.Source);
        }
    }

    public static string FormatName(UnitWard ward)
    {
        return ward.Accept(NameVisitor.Instance, Empty.Value);
    }

    public static string FormatList(ImmutableArray<UnitWard> wards, UnitWardCategory category)
    {
        JoinStringBuilder names = new(", ");

        foreach (UnitWard ward in wards)
        {
            if (ward.Category == category)
            {
                names.Add(FormatName(ward));
            }
        }

        if (names.Count > 0)
        {
            return names.ToString();
        }
        else
        {
            return Tr("None");
        }
    }
}
