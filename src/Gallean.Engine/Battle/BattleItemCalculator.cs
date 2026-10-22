using System.Diagnostics.CodeAnalysis;
using Gallean.Common;

namespace Gallean.Engine;

internal static partial class BattleItemCalculator
{
    public sealed record ItemInfo(
        IReadOnlyList<BattleAttackTarget> Targets,
        IReadOnlyList<BattleEvent> Events,
        BattleCommandError? Error)
    {
        [MemberNotNullWhen(true, nameof(Error))]
        public bool Failed => Error is not null;

        public static ItemInfo Fail(BattleCommandError error)
        {
            return new ItemInfo([], [], error);
        }
    }

    public sealed record UseInfo(BattleAttackTarget Target, ItemInfo Info);

    public sealed record SlotInfo(LeaderEquipmentSlot Slot, IReadOnlyList<UseInfo> Uses);

    public sealed record UserInfo(IReadOnlyList<SlotInfo> Slots, BattleCommandError? Error)
    {
        [MemberNotNullWhen(true, nameof(Error))]
        public bool Failed => Error is not null;

        public static UserInfo Fail(BattleCommandError error)
        {
            return new UserInfo([], error);
        }
    }

    public static UserInfo GetUserInfo(BattleState state, IEngineContext context, BattleUnitId unitId)
    {
        BattleUnit unit = state.GetUnit(unitId);

        if (!IsStackLeader(unit, context.Definitions))
        {
            return UserInfo.Fail(BattleCommandError.ItemUserNotLeader);
        }

        List<SlotInfo> slots = [];

        foreach (BattleItem item in state.Items)
        {
            if (item.Side == unit.Side && !item.Consumed)
            {
                List<UseInfo> uses = [];

                foreach (BattleAttackTarget target in EnumerateAttackTargets(state))
                {
                    uses.Add(new UseInfo(target, GetUserItemInfo(state, context, unit, item.Slot, target)));
                }

                slots.Add(new SlotInfo(item.Slot, uses));
            }
        }

        return new UserInfo(slots, null);
    }

    public static ItemInfo GetItemInfo(
        BattleState state,
        IEngineContext context,
        BattleUnitId unitId,
        LeaderEquipmentSlot slot,
        BattleAttackTarget target)
    {
        BattleUnit unit = state.GetUnit(unitId);

        if (!IsStackLeader(unit, context.Definitions))
        {
            return ItemInfo.Fail(BattleCommandError.ItemUserNotLeader);
        }

        return GetUserItemInfo(state, context, unit, slot, target);
    }

    private static ItemInfo GetUserItemInfo(
        BattleState state,
        IEngineContext context,
        BattleUnit unit,
        LeaderEquipmentSlot slot,
        BattleAttackTarget target)
    {
        if (!IsAvailableInTurn(state.CurrentUnitTurn.Type))
        {
            return ItemInfo.Fail(BattleCommandError.ActionNotAvailable);
        }

        if (!TryGetItem(state, unit.Side, slot, out BattleItem? item))
        {
            return ItemInfo.Fail(BattleCommandError.ItemNotEquipped);
        }

        if (item.Consumed)
        {
            return ItemInfo.Fail(BattleCommandError.ItemNotEquipped);
        }

        ItemDefinition itemDefinition = context.Definitions.GetItem(item.ItemId);

        if (!HasRequiredAbility(state, context, unit.Id, itemDefinition))
        {
            return ItemInfo.Fail(BattleCommandError.MissingLeaderAbility);
        }

        if (itemDefinition.Effect is null)
        {
            return ItemInfo.Fail(BattleCommandError.ItemNotUsable);
        }

        if (!IsAvailableInRound(state.CurrentRound.Type, itemDefinition.Effect, context.Definitions))
        {
            return ItemInfo.Fail(BattleCommandError.ItemNotUsable);
        }

        BattleItemResolver.Context resolveContext = new(state, context, unit, target);
        ItemInfo effectInfo = itemDefinition.Effect.Accept(BattleItemResolver.Instance, resolveContext);

        if (effectInfo.Failed)
        {
            return effectInfo;
        }

        int? remainingCharges;
        bool consumed;

        if (item.Charges is not null)
        {
            remainingCharges = item.Charges.Value - 1;
            consumed = remainingCharges <= 0;
        }
        else
        {
            remainingCharges = null;
            consumed = LeaderEquipmentCalculator.IsConsumable(itemDefinition.Category);
        }

        BattleItemUsedEvent usedEvent = new(unit.Side, slot, item.ItemId, unit.Id, target, remainingCharges, consumed);

        return new ItemInfo(effectInfo.Targets, [usedEvent, .. effectInfo.Events], null);
    }

    private static bool IsStackLeader(BattleUnit unit, IGameDefinitionProvider definitions)
    {
        if (unit.SummonerId is not null || unit.HasStatusEffect<BattleTransformStatusEffect>())
        {
            return false;
        }

        UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

        return definition.Leadership is > 0;
    }

    private static bool TryGetItem(BattleState state, BattleSide side, LeaderEquipmentSlot slot, [NotNullWhen(true)] out BattleItem? result)
    {
        foreach (BattleItem item in state.Items)
        {
            if (item.Side == side && item.Slot == slot)
            {
                result = item;
                return true;
            }
        }

        result = null;
        return false;
    }

    private static bool HasRequiredAbility(BattleState state, IEngineContext context, BattleUnitId unitId, ItemDefinition itemDefinition)
    {
        LeaderAbility? requiredAbility = LeaderEquipmentCalculator.GetRequiredAbility(itemDefinition.Category);

        if (requiredAbility is not null)
        {
            LeaderAbility abilities = context.UnitStats.GetBattleUnitAbilities(state, unitId);
            return abilities.HasFlag(requiredAbility.Value);
        }
        else
        {
            return true;
        }
    }

    private static bool IsAvailableInTurn(BattleUnitTurnType type)
    {
        return type switch
        {
            BattleUnitTurnType.Normal => true,
            BattleUnitTurnType.SecondAttack => false,
            BattleUnitTurnType.GrantedAttack => true,
            BattleUnitTurnType.Resumed => true,
            BattleUnitTurnType.Doppelganger => true
        };
    }

    private static bool IsAvailableInRound(BattleRoundType type, ItemEffect effect, IGameDefinitionProvider definitions)
    {
        return type switch
        {
            BattleRoundType.Opening => true,
            BattleRoundType.Normal => true,
            BattleRoundType.Recovery => effect.Accept(RecoveryEffectVisitor.Instance, definitions)
        };
    }

    private static IEnumerable<BattleAttackTarget> EnumerateAttackTargets(BattleState state)
    {
        foreach (BattleUnitId id in state.GetAllUnitIds())
        {
            yield return new BattleAttackUnitTarget(id);
        }

        foreach (BattleSide side in Enum.GetValues<BattleSide>())
        {
            UnitGrid<BattleUnit> grid = state.GetGrid(side);

            foreach (GridPosition position in GridPosition.All)
            {
                if (grid[position] is null)
                {
                    yield return new BattleAttackTileTarget(side, position);
                }
            }
        }
    }
}
