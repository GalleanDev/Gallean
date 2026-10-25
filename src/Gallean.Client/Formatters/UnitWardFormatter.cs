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
}
