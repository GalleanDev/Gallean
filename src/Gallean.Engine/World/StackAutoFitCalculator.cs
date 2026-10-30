using Gallean.Common;

namespace Gallean.Engine;

internal static partial class StackAutoFitCalculator
{
    private static readonly LeaderEquipmentSlot[] EquipmentSlots = Enum.GetValues<LeaderEquipmentSlot>();

    private static readonly IReadOnlyList<GridPosition> FrontOrder =
    [
        GridPosition.FrontCenter,
        GridPosition.FrontBottom,
        GridPosition.FrontTop,
        GridPosition.BackCenter,
        GridPosition.BackBottom,
        GridPosition.BackTop
    ];

    private static readonly IReadOnlyList<GridPosition> BackOrder =
    [
        GridPosition.BackCenter,
        GridPosition.BackBottom,
        GridPosition.BackTop,
        GridPosition.FrontCenter,
        GridPosition.FrontBottom,
        GridPosition.FrontTop
    ];

    public static Result Calculate(
        UnitGrid<WorldUnit> stackUnits,
        UnitGrid<WorldUnit> garrisonUnits,
        WorldInventory stackInventory,
        WorldInventory garrisonInventory,
        LeaderEquipment equipment,
        int garrisonCapacity,
        IGameDefinitionProvider definitions)
    {
        List<Candidate> candidates = [];

        foreach ((WorldUnit unit, GridPosition _) in stackUnits.GetUnits())
        {
            candidates.Add(CreateCandidate(unit, isFromStack: true, definitions));
        }

        foreach ((WorldUnit unit, GridPosition _) in garrisonUnits.GetUnits())
        {
            candidates.Add(CreateCandidate(unit, isFromStack: false, definitions));
        }

        candidates.Sort();

        List<Selection> selections = BuildSelections(candidates);
        selections.Sort();

        foreach (Selection selection in selections)
        {
            UnitGrid<WorldUnit>? selectionUnits = TryBuildStackUnits(selection.Candidates, definitions);
            if (selectionUnits is null)
            {
                continue;
            }

            List<Candidate> remaining = [];
            foreach (Candidate candidate in candidates)
            {
                if (!selection.Candidates.Contains(candidate))
                {
                    remaining.Add(candidate);
                }
            }

            UnitGrid<WorldUnit>? remainingUnits = TryBuildGarrisonUnits(remaining, garrisonUnits, garrisonCapacity, definitions);
            if (remainingUnits is null)
            {
                continue;
            }

            return ComputeEquipment(
                selectionUnits,
                remainingUnits,
                stackInventory,
                garrisonInventory,
                equipment,
                definitions);
        }

        return new Result(stackUnits, garrisonUnits, stackInventory, garrisonInventory, equipment);
    }

    private static Result ComputeEquipment(
        UnitGrid<WorldUnit> stackUnits,
        UnitGrid<WorldUnit> garrisonUnits,
        WorldInventory stackInventory,
        WorldInventory garrisonInventory,
        LeaderEquipment equipment,
        IGameDefinitionProvider definitions)
    {
        List<WorldItem> pool = [];

        foreach (LeaderEquipmentSlot slot in EquipmentSlots)
        {
            if (equipment[slot] is { } equippedItem)
            {
                pool.Add(equippedItem);
            }
        }

        pool.AddRange(stackInventory.Items);
        pool.AddRange(garrisonInventory.Items);

        UnitGrid<WorldUnit> updatedGarrison = LeaderEquipmentCalculator.UpdateUnitModifiers(garrisonUnits, LeaderEquipment.Empty, definitions);
        UnitGrid<WorldUnit> updatedStack = LeaderEquipmentCalculator.UpdateUnitModifiers(stackUnits, LeaderEquipment.Empty, definitions);
        LeaderAbility? leaderAbilities = TryGetLeaderAbilities(updatedStack, definitions);

        if (leaderAbilities is null)
        {
            return new Result(
                updatedStack,
                updatedGarrison,
                new WorldInventory([.. pool]),
                WorldInventory.Empty,
                LeaderEquipment.Empty);
        }

        List<(ItemDefinition Definition, int PoolIndex, EquipmentCategory Category, int Score)> equippableItems = [];

        for (int index = 0; index < pool.Count; index++)
        {
            ItemDefinition item = definitions.GetItem(pool[index].DefinitionId);

            if (IsEquippable(item, leaderAbilities.Value))
            {
                equippableItems.Add((item, index, GetEquipmentCategory(item.Category), GetItemScore(item)));
            }
        }

        equippableItems.Sort(CompareItem);

        LeaderEquipment updatedEquipment = LeaderEquipment.Empty;
        HashSet<int> equippedIndexes = [];

        foreach ((ItemDefinition item, int poolIndex, _, _) in equippableItems)
        {
            LeaderEquipmentSlot? slot = FindEquipmentSlot(updatedEquipment, item.Category);

            if (slot is not null)
            {
                updatedEquipment = updatedEquipment.With(slot.Value, pool[poolIndex]);
                equippedIndexes.Add(poolIndex);
            }
        }

        UnitGrid<WorldUnit> finalStack = LeaderEquipmentCalculator.UpdateUnitModifiers(stackUnits, updatedEquipment, definitions);
        List<WorldItem> remaining = [];

        for (int index = 0; index < pool.Count; index++)
        {
            if (!equippedIndexes.Contains(index))
            {
                remaining.Add(pool[index]);
            }
        }

        WorldInventory updatedInventory = new([.. remaining]);

        return new Result(finalStack, updatedGarrison, updatedInventory, WorldInventory.Empty, updatedEquipment);
    }

    private static LeaderAbility? TryGetLeaderAbilities(UnitGrid<WorldUnit> units, IGameDefinitionProvider definitions)
    {
        foreach ((WorldUnit unit, _) in units.GetUnits())
        {
            UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

            if (definition.Leadership is > 0)
            {
                return UnitStatsCalculator.GetLeaderAbilities(definition, unit.Modifiers, definitions);
            }
        }

        return null;
    }

    private static int CompareItem(
        (ItemDefinition Definition, int PoolIndex, EquipmentCategory Category, int Score) left,
        (ItemDefinition Definition, int PoolIndex, EquipmentCategory Category, int Score) right)
    {
        int rv;

        rv = GetEquipmentOrder(left.Category).CompareTo(GetEquipmentOrder(right.Category));
        if (rv != 0)
        {
            return rv;
        }

        rv = right.Score.CompareTo(left.Score);
        if (rv != 0)
        {
            return rv;
        }

        rv = string.CompareOrdinal(left.Definition.Id.Value, right.Definition.Id.Value);
        if (rv != 0)
        {
            return rv;
        }

        rv = left.PoolIndex.CompareTo(right.PoolIndex);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }

    private static bool IsEquippable(ItemDefinition item, LeaderAbility leaderAbilities)
    {
        EquipmentCategory category = GetEquipmentCategory(item.Category);

        if (CanEquip(category) &&
            EquipmentSlots.Any(slot => LeaderEquipmentCalculator.CanPlace(item.Category, slot)))
        {
            LeaderAbility? requiredAbility = LeaderEquipmentCalculator.GetRequiredAbility(item.Category);

            return requiredAbility is null || leaderAbilities.HasFlag(requiredAbility.Value);
        }
        else
        {
            return false;
        }
    }

    private static LeaderEquipmentSlot? FindEquipmentSlot(LeaderEquipment equipment, ItemCategory category)
    {
        foreach (LeaderEquipmentSlot slot in EquipmentSlots)
        {
            if (equipment[slot] is null && LeaderEquipmentCalculator.CanPlace(category, slot))
            {
                return slot;
            }
        }

        return null;
    }

    private static bool CanEquip(EquipmentCategory category)
    {
        return category switch
        {
            EquipmentCategory.Active => true,
            EquipmentCategory.Healing => true,
            EquipmentCategory.Other => true,
            EquipmentCategory.Revive => false
        };
    }

    private static EquipmentCategory GetEquipmentCategory(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Orb => EquipmentCategory.Active,
            ItemCategory.Talisman => EquipmentCategory.Active,
            ItemCategory.PotionHeal => EquipmentCategory.Healing,
            ItemCategory.Armor => EquipmentCategory.Other,
            ItemCategory.Jewel => EquipmentCategory.Other,
            ItemCategory.Weapon => EquipmentCategory.Other,
            ItemCategory.Banner => EquipmentCategory.Other,
            ItemCategory.PotionTemporaryBoost => EquipmentCategory.Other,
            ItemCategory.PotionRevive => EquipmentCategory.Revive,
            ItemCategory.PotionPermanentBoost => EquipmentCategory.Other,
            ItemCategory.Scroll => EquipmentCategory.Other,
            ItemCategory.Wand => EquipmentCategory.Other,
            ItemCategory.Valuable => EquipmentCategory.Other,
            ItemCategory.TravelItem => EquipmentCategory.Other,
            ItemCategory.Special => EquipmentCategory.Other
        };
    }

    private static int GetEquipmentOrder(EquipmentCategory category)
    {
        return category switch
        {
            EquipmentCategory.Active => 0,
            EquipmentCategory.Healing => 1,
            EquipmentCategory.Other => 2,
            EquipmentCategory.Revive => 3
        };
    }

    private static int GetItemScore(ItemDefinition item)
    {
        WorldResourceSet value = item.Value;

        return value.Gold +
               value.DeathMana +
               value.RuneMana +
               value.LifeMana +
               value.InfernalMana +
               value.GroveMana;
    }

    private static Candidate CreateCandidate(
        WorldUnit unit,
        bool isFromStack,
        IGameDefinitionProvider definitions)
    {
        UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

        return new Candidate(
            unit,
            definition,
            isFromStack,
            definition.IsLarge ? GridPosition.LineCount : 1);
    }

    private static List<Selection> BuildSelections(IReadOnlyList<Candidate> candidates)
    {
        List<Selection> selections = [new([], 0, 0, 0, 0, 0)];

        foreach (Candidate candidate in candidates)
        {
            int existingSelectionCount = selections.Count;

            for (int index = 0; index < existingSelectionCount; index++)
            {
                Selection selection = selections[index];
                int size = selection.Size + candidate.Size;
                if (size <= GridPosition.Count)
                {
                    IReadOnlyList<Candidate> newCandidates = [.. selection.Candidates, candidate];

                    selections.Add(new Selection(
                        newCandidates,
                        size,
                        selection.Gold + candidate.Definition.PurchaseCost.Gold,
                        selection.Experience + candidate.Unit.Experience,
                        selection.Hitpoints + candidate.Unit.Hitpoints,
                        selection.StackUnits + (candidate.IsFromStack ? 1 : 0)));
                }
            }
        }

        return selections;
    }

    private static UnitGrid<WorldUnit>? TryBuildStackUnits(
        IReadOnlyList<Candidate> candidates,
        IGameDefinitionProvider definitions)
    {
        IReadOnlyList<(Candidate Candidate, GridPosition Position)>? layout = ComputeLayout(candidates);

        if (layout is null)
        {
            return null;
        }

        WorldStackBuilder builder = new(definitions);

        foreach ((Candidate candidate, GridPosition position) in layout)
        {
            builder.PlaceUnit(candidate.Unit, position);
        }

        return builder.TryBuild(out UnitGrid<WorldUnit>? units, out _) ? units : null;
    }

    private static UnitGrid<WorldUnit>? TryBuildGarrisonUnits(
        IReadOnlyList<Candidate> candidates,
        UnitGrid<WorldUnit> garrisonUnits,
        int garrisonCapacity,
        IGameDefinitionProvider definitions)
    {
        IReadOnlyList<(Candidate Candidate, GridPosition Position)>? layout = ComputeLayout(candidates);

        if (layout is null)
        {
            return null;
        }

        WorldGarrisonBuilder builder = new(definitions, garrisonUnits, garrisonCapacity);

        foreach ((Candidate candidate, GridPosition position) in layout)
        {
            builder.PlaceUnit(candidate.Unit, position);
        }

        return builder.TryBuild(out UnitGrid<WorldUnit>? units, out _) ? units : null;
    }

    private static IReadOnlyList<(Candidate Candidate, GridPosition Position)>? ComputeLayout(IReadOnlyList<Candidate> candidates)
    {
        List<(Candidate Candidate, GridPosition Position)> result = [];
        HashSet<GridPosition> occupied = [];

        List<Candidate> ordered = [.. candidates];
        ordered.Sort();

        foreach (Candidate candidate in ordered)
        {
            if (candidate.Definition.IsLarge)
            {
                GridPosition? target = FindLargePosition(occupied, FrontOrder);

                if (target is null)
                {
                    return null;
                }

                result.Add((candidate, target));
                occupied.Add(target);
                occupied.Add(GridPosition.AtBackLine(target.Flank));
            }
            else
            {
                IReadOnlyList<GridPosition> positions;

                if (candidate.Definition.Attack.Reach is UnitAttackReach.Any or UnitAttackReach.All)
                {
                    positions = BackOrder;
                }
                else
                {
                    positions = FrontOrder;
                }

                GridPosition? target = FindSmallPosition(occupied, positions);

                if (target is null)
                {
                    return null;
                }

                result.Add((candidate, target));
                occupied.Add(target);
            }
        }

        return result;
    }

    private static GridPosition? FindLargePosition(HashSet<GridPosition> occupied, IReadOnlyList<GridPosition> positions)
    {
        foreach (GridPosition position in positions)
        {
            if (position.IsFrontLine &&
                !occupied.Contains(position) &&
                !occupied.Contains(GridPosition.AtBackLine(position.Flank)))
            {
                return position;
            }
        }

        return null;
    }

    private static GridPosition? FindSmallPosition(HashSet<GridPosition> occupied, IReadOnlyList<GridPosition> positions)
    {
        foreach (GridPosition position in positions)
        {
            if (!occupied.Contains(position))
            {
                return position;
            }
        }

        return null;
    }
}
