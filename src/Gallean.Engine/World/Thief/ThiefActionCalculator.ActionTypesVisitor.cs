using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal static partial class ThiefCalculator
{
    private sealed class ActionTypesVisitor :
        IThiefTargetVisitor<Empty, ImmutableArray<WorldThiefActionType>>,
        IStaticInstance<ActionTypesVisitor>
    {
        public ImmutableArray<WorldThiefActionType> Visit(ThiefStackTarget target, Empty context)
        {
            return
            [
                WorldThiefActionType.PoisonStack,
                WorldThiefActionType.Spy,
                WorldThiefActionType.StealItem,
                WorldThiefActionType.Assassinate,
                WorldThiefActionType.CounterfeitOrders,
                WorldThiefActionType.Duel
            ];
        }

        public ImmutableArray<WorldThiefActionType> Visit(ThiefFortificationTarget target, Empty context)
        {
            if (target.Fortification is WorldCity)
            {
                return
                [
                    WorldThiefActionType.Spy,
                    WorldThiefActionType.PoisonCity,
                    WorldThiefActionType.StealGold,
                    WorldThiefActionType.StealSpell,
                    WorldThiefActionType.Assassinate,
                    WorldThiefActionType.CounterfeitOrders,
                    WorldThiefActionType.Bribe,
                    WorldThiefActionType.RiotCity
                ];
            }
            else
            {
                return
                [
                    WorldThiefActionType.Spy,
                    WorldThiefActionType.PoisonCity,
                    WorldThiefActionType.StealGold,
                    WorldThiefActionType.StealSpell,
                    WorldThiefActionType.CounterfeitOrders
                ];
            }
        }

        public ImmutableArray<WorldThiefActionType> Visit(ThiefMerchantSiteTarget target, Empty context)
        {
            return [WorldThiefActionType.StealMerchant];
        }

        public ImmutableArray<WorldThiefActionType> Visit(ThiefMageSiteTarget target, Empty context)
        {
            return [WorldThiefActionType.StealMage];
        }

        public ImmutableArray<WorldThiefActionType> Visit(ThiefRuinTarget target, Empty context)
        {
            return [WorldThiefActionType.SpyRuin];
        }
    }
}
