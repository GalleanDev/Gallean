using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleItemUsedEvent ev, BattleState state)
    {
        ImmutableArray<BattleItem>.Builder items = state.Items.ToBuilder();

        for (int index = 0; index < items.Count; index++)
        {
            BattleItem item = items[index];

            if (item.Side == ev.Side && item.Slot == ev.Slot)
            {
                items[index] = item with { Used = true };
            }
        }

        return state with { Items = items.ToImmutable() };
    }
}
