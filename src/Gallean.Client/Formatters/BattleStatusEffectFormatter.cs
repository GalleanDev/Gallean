using Gallean.Common;

namespace Gallean.Client;

internal static class BattleStatusEffectFormatter
{
    private sealed class NameVisitor : IBattleStatusEffectVisitor<Empty, string>, IStaticInstance<NameVisitor>
    {
        public string Visit(BattleDefendStatusEffect effect, Empty context)
        {
            return Tr("Defending");
        }

        public string Visit(BattleParalyzeStatusEffect effect, Empty context)
        {
            return Tr("Paralyzed");
        }

        public string Visit(BattlePetrifyStatusEffect effect, Empty context)
        {
            return Tr("Petrified");
        }

        public string Visit(BattlePoisonStatusEffect effect, Empty context)
        {
            return Tr("Poisoned");
        }

        public string Visit(BattleFrostbiteStatusEffect effect, Empty context)
        {
            return Tr("Frostbitten");
        }

        public string Visit(BattleBlisterStatusEffect effect, Empty context)
        {
            return Tr("Blistered");
        }

        public string Visit(BattleRetreatStatusEffect effect, Empty context)
        {
            return Tr("Retreating");
        }

        public string Visit(BattleRetreatedStatusEffect effect, Empty context)
        {
            return Tr("Retreated");
        }

        public string Visit(BattleFortificationStatusEffect effect, Empty context)
        {
            return Tr("Fortified");
        }

        public string Visit(BattleIncreaseDamageStatusEffect effect, Empty context)
        {
            return Tr("Damage boosted");
        }

        public string Visit(BattleReduceDamageStatusEffect effect, Empty context)
        {
            return Tr("Damage reduced");
        }

        public string Visit(BattleReduceInitiativeStatusEffect effect, Empty context)
        {
            return Tr("Initiative reduced");
        }

        public string Visit(BattleShatterArmorStatusEffect effect, Empty context)
        {
            return Tr("Armor shattered");
        }

        public string Visit(BattleCureStatusEffect effect, Empty context)
        {
            return Tr("Cured");
        }

        public string Visit(BattleTransformStatusEffect effect, Empty context)
        {
            return effect.Type switch
            {
                BattleTransformType.Transform => Tr("Transformed"),
                BattleTransformType.TransformSelf => Tr("Transformed"),
                BattleTransformType.Doppelganger => Tr("Doppelganger"),
                BattleTransformType.LevelDrain => Tr("Level drained")
            };
        }
    }

    public static string FormatName(BattleAbstractStatusEffect effect)
    {
        return effect.Accept(NameVisitor.Instance, Empty.Value);
    }
}
