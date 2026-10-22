using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Gallean.Engine;

internal static class WorldCommandHelpers
{
    public static bool TryGetCurrentPlayerStack(WorldState state, WorldStackId stackId, [NotNullWhen(true)] out WorldStack? stack)
    {
        if (state.Stacks.TryGetValue(stackId, out stack) && stack.OwnerId == state.TurnInfo.CurrentPlayerId)
        {
            return true;
        }
        else
        {
            stack = null;

            return false;
        }
    }

    public static bool TryGetCurrentPlayerFortification(
        WorldState state,
        WorldFortificationId fortificationId,
        [NotNullWhen(true)] out WorldFortification? fortification)
    {
        if (state.Fortifications.TryGetValue(fortificationId, out fortification) &&
            FortificationCalculator.GetOwner(fortification) == state.TurnInfo.CurrentPlayerId)
        {
            return true;
        }
        else
        {
            fortification = null;

            return false;
        }
    }

    public static WorldStackId GenerateStackId(WorldState state)
    {
        HashSet<WorldStackId> usedIds = [];

        usedIds.UnionWith(state.Stacks.Keys);

        foreach (ScenarioDestroyedStack stack in state.ScenarioState.DestroyedStacks)
        {
            usedIds.Add(stack.StackId);
        }

        int index = 1;

        while (usedIds.Contains(new WorldStackId($"Stack{index:D4}")))
        {
            index++;
        }

        return new WorldStackId($"Stack{index:D4}");
    }

    public static WorldBagId GenerateBagId(WorldState state)
    {
        int index = 1;

        while (state.Bags.ContainsKey(new WorldBagId($"Bag{index:D4}")))
        {
            index++;
        }

        return new WorldBagId($"Bag{index:D4}");
    }

    public static WorldTombId GenerateTombId(WorldState state)
    {
        int index = 1;

        while (state.Tombs.ContainsKey(new WorldTombId($"Tomb{index:D4}")))
        {
            index++;
        }

        return new WorldTombId($"Tomb{index:D4}");
    }

    public static WorldItemId GenerateItemId(WorldState state)
    {
        HashSet<WorldItemId> usedIds = [];

        foreach (WorldItem item in EnumerateItems(state))
        {
            usedIds.Add(item.Id);
        }

        foreach (WorldPlayer player in state.Players.Values)
        {
            usedIds.UnionWith(player.TurnActions.ItemUses.Keys);
        }

        int index = 1;

        while (usedIds.Contains(new WorldItemId($"Item{index:D4}")))
        {
            index++;
        }

        return new WorldItemId($"Item{index:D4}");
    }

    public static IEnumerable<WorldUnitId> GenerateUnitIds(WorldState state)
    {
        HashSet<WorldUnitId> usedIds = [];

        foreach (WorldStack stack in state.Stacks.Values)
        {
            foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
            {
                usedIds.Add(unit.Id);
            }
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            foreach ((WorldUnit unit, GridPosition _) in fortification.Garrison.GetUnits())
            {
                usedIds.Add(unit.Id);
            }
        }

        for (int index = 1;; index++)
        {
            WorldUnitId id = new($"Unit{index:D4}");

            if (!usedIds.Contains(id))
            {
                yield return id;
            }
        }
    }

    private static IEnumerable<WorldItem> EnumerateItems(WorldState state)
    {
        foreach (WorldStack stack in state.Stacks.Values)
        {
            foreach (WorldItem item in stack.Inventory.Items)
            {
                yield return item;
            }

            foreach (WorldItem item in stack.Equipment.Items.Values)
            {
                yield return item;
            }
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            foreach (WorldItem item in fortification.Inventory.Items)
            {
                yield return item;
            }
        }

        foreach (WorldBag bag in state.Bags.Values)
        {
            foreach (WorldItem item in bag.Inventory.Items)
            {
                yield return item;
            }
        }
    }

    public static WorldSubraceId GetPlayerSubraceId(WorldState state, WorldPlayerId playerId)
    {
        foreach (WorldSubrace subrace in state.Subraces.Values)
        {
            if (subrace.PlayerId == playerId)
            {
                return subrace.Id;
            }
        }

        throw new InvalidOperationException($"Player '{playerId.Value}' has no subrace.");
    }

    public static WorldState RemoveStack(
        WorldState state,
        WorldStackId stackId,
        WorldUnitGroup? destroyer,
        WorldPlayerId? destroyerPlayerId,
        GlobalVariables globals)
    {
        WorldPlan plan = WorldPlan.For(state);

        WorldStack stack = state.Stacks[stackId];
        ImmutableArray<WorldItem> items = [.. stack.Inventory.Items, .. stack.Equipment.Items.Values];
        WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

        ImmutableSortedDictionary<WorldStackId, WorldStack> updatedStacks = state.Stacks.Remove(stackId);

        WorldScenarioState updatedScenarioState = state.ScenarioState with
        {
            DestroyedStacks = state.ScenarioState.DestroyedStacks.Add(
                new ScenarioDestroyedStack(stackId, destroyerPlayerId, stack.SourceTemplateId))
        };

        state = state with { Stacks = updatedStacks, ScenarioState = updatedScenarioState };

        if (destroyer is not null)
        {
            WorldInventory inventory = WorldUnitGroupCalculator.GetInventory(state, destroyer);

            inventory = inventory.AddRange(items);

            state = WorldUnitGroupCalculator.WithInventory(state, destroyer, inventory);
        }
        else if (fortification is not null)
        {
            WorldFortification updatedFortification = fortification with { Inventory = fortification.Inventory.AddRange(items) };

            state = state with
            {
                Fortifications = state.Fortifications.SetItem(updatedFortification.Id, updatedFortification)
            };
        }
        else if (!items.IsEmpty)
        {
            WorldBag? bag = plan.GetBagAt(stack.Position);
            WorldBag updatedBag;

            if (bag is not null)
            {
                updatedBag = bag with { Inventory = bag.Inventory.AddRange(items) };
            }
            else
            {
                WorldBagId bagId = GenerateBagId(state);

                updatedBag = new WorldBag(
                    bagId,
                    stack.Position,
                    new WorldObjectVariant(0),
                    new WorldInventory(items),
                    globals.AiDefaultPriority);
            }

            state = state with
            {
                Bags = state.Bags.SetItem(updatedBag.Id, updatedBag)
            };
        }

        return state;
    }

    public static WorldStackMovingEvent CreateMoveStepEvent(
        WorldStack stack,
        WorldPlan plan,
        WorldMovementCalculator.MovementProfile profile,
        ImmutableArray<WorldPosition> remainingPath,
        WorldPosition? encounterPosition)
    {
        WorldPosition nextPosition = remainingPath[0];
        WorldStackDirection direction = StackStepCalculator.GetDirection(stack.Position, nextPosition);
        int stepCost = plan.GetStepCost(stack.Position, nextPosition, profile);
        int updatedMovementPoints = Math.Max(0, stack.MovementPoints - stepCost);

        return new WorldStackMovingEvent(
            stack.Id,
            stack.Position,
            nextPosition,
            direction,
            updatedMovementPoints,
            remainingPath[1..],
            encounterPosition);
    }

    public static List<WorldEvent> CreateDestroyIllusionEvents(WorldStack stack, WorldStack illusion, IEngineContext context)
    {
        StackStats stackStats = context.UnitStats.GetWorldStackStats(stack);
        int actionCost = WorldMovementCalculator.GetActionCost(stackStats, context.Globals);
        int movementPoints = Math.Max(0, stack.MovementPoints - actionCost);

        return
        [
            new WorldStackDestroyedEvent(illusion.Id, new WorldUnitGroup.Stack(stack.Id), stack.OwnerId),
            new WorldStackMovementConsumedEvent(stack.Id, movementPoints)
        ];
    }

    public static List<WorldEvent> CreateBattleStartEvents(
        WorldState state,
        WorldStack stack,
        WorldStack? enemyStack,
        WorldFortification? enemyFortification,
        IEngineContext context)
    {
        StackStats stackStats = context.UnitStats.GetWorldStackStats(stack);
        int actionCost = WorldMovementCalculator.GetActionCost(stackStats, context.Globals);
        int movementPoints = Math.Max(0, stack.MovementPoints - actionCost);
        (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(context.Definitions);

        List<WorldEvent> events = [new WorldStackMovementConsumedEvent(stack.Id, movementPoints)];

        if (leaderDefinition.Category != UnitCategory.Illusion)
        {
            if (enemyStack is not null)
            {
                BattleModifiers modifiers = CreateModifiers(state, stack.OwnerId, enemyStack.OwnerId, context);
                BattleState battleState = CreateStackBattleState(stack, enemyStack, modifiers, context);

                events.Add(new WorldStackBattleStartedEvent(stack.Id, enemyStack.Id, enemyStack.Position, battleState, IsDuel: false));
            }
            else if (enemyFortification is not null)
            {
                WorldPlayerId? fortificationOwnerId = FortificationCalculator.GetOwner(enemyFortification);

                BattleModifiers modifiers = CreateModifiers(state, stack.OwnerId, fortificationOwnerId, context);
                int fortificationArmor = FortificationCalculator.GetArmorBonus(enemyFortification, context.Globals);
                BattleState battleState = CreateGarrisonBattleState(
                    stack, enemyFortification.Garrison, fortificationArmor, modifiers, context);

                events.Add(new WorldFortificationBattleStartedEvent(stack.Id, enemyFortification.Id, enemyFortification.Entrance, battleState));
            }
        }

        return events;
    }

    public static BattleModifiers CreateModifiers(
        WorldState state,
        WorldPlayerId? attackerOwnerId,
        WorldPlayerId? defenderOwnerId,
        IEngineContext context)
    {
        return new BattleModifiers(
            WorldBattleModifierCalculator.GetModifiers(state, attackerOwnerId, context),
            WorldBattleModifierCalculator.GetModifiers(state, defenderOwnerId, context));
    }

    public static BattleState CreateStackBattleState(
        WorldStack attackerStack,
        WorldStack defenderStack,
        BattleModifiers modifiers,
        IEngineContext context)
    {
        BattleStateBuilder builder = new(context.Definitions);
        int index = 0;

        foreach ((WorldUnit unit, GridPosition position) in attackerStack.Units.GetUnits())
        {
            BattleUnit newUnit = new(
                new BattleUnitId(index),
                unit.Id,
                unit.DefinitionId,
                BattleSide.Attacker,
                unit.Hitpoints,
                unit.Experience,
                0,
                unit.Level,
                unit.Modifiers,
                [],
                null,
                GetEquipmentAttackIds(unit, attackerStack, context),
                unit.Name,
                unit.IsTypeLocked);

            builder.Add(newUnit, position);
            index++;
        }

        foreach ((WorldUnit unit, GridPosition position) in defenderStack.Units.GetUnits())
        {
            BattleUnit newUnit = new(
                new BattleUnitId(index),
                unit.Id,
                unit.DefinitionId,
                BattleSide.Defender,
                unit.Hitpoints,
                unit.Experience,
                0,
                unit.Level,
                unit.Modifiers,
                [],
                null,
                GetEquipmentAttackIds(unit, defenderStack, context),
                unit.Name,
                unit.IsTypeLocked);

            builder.Add(newUnit, position);
            index++;
        }

        AddBattleItems(builder, BattleSide.Attacker, attackerStack);
        AddBattleItems(builder, BattleSide.Defender, defenderStack);

        return builder.Build(modifiers);
    }

    public static BattleState CreateDuelBattleState(
        WorldStack attackerStack,
        WorldStack defenderStack,
        BattleModifiers modifiers,
        IEngineContext context)
    {
        (WorldUnit attackerLeader, GridPosition _, UnitDefinition _) = attackerStack.GetLeader(context.Definitions);
        (WorldUnit defenderLeader, GridPosition _, UnitDefinition _) = defenderStack.GetLeader(context.Definitions);

        BattleUnit attackerUnit = new(
            new BattleUnitId(0),
            attackerLeader.Id,
            attackerLeader.DefinitionId,
            BattleSide.Attacker,
            attackerLeader.Hitpoints,
            attackerLeader.Experience,
            0,
            attackerLeader.Level,
            attackerLeader.Modifiers,
            [],
            null,
            GetEquipmentAttackIds(attackerLeader, attackerStack, context),
            attackerLeader.Name,
            attackerLeader.IsTypeLocked);

        BattleUnit defenderUnit = new(
            new BattleUnitId(1),
            defenderLeader.Id,
            defenderLeader.DefinitionId,
            BattleSide.Defender,
            defenderLeader.Hitpoints,
            defenderLeader.Experience,
            0,
            defenderLeader.Level,
            defenderLeader.Modifiers,
            [],
            null,
            GetEquipmentAttackIds(defenderLeader, defenderStack, context),
            defenderLeader.Name,
            defenderLeader.IsTypeLocked);

        BattleStateBuilder builder = new(context.Definitions);

        builder.Add(attackerUnit, GridPosition.FrontCenter);
        builder.Add(defenderUnit, GridPosition.FrontCenter);

        AddBattleItems(builder, BattleSide.Attacker, attackerStack);
        AddBattleItems(builder, BattleSide.Defender, defenderStack);

        return builder.Build(modifiers);
    }

    public static BattleState CreateGarrisonBattleState(
        WorldStack attackerStack,
        UnitGrid<WorldUnit> defenderUnits,
        int fortificationArmor,
        BattleModifiers modifiers,
        IEngineContext context)
    {
        BattleStateBuilder builder = new(context.Definitions);
        int index = 0;

        foreach ((WorldUnit unit, GridPosition position) in attackerStack.Units.GetUnits())
        {
            BattleUnit newUnit = new(
                new BattleUnitId(index),
                unit.Id,
                unit.DefinitionId,
                BattleSide.Attacker,
                unit.Hitpoints,
                unit.Experience,
                0,
                unit.Level,
                unit.Modifiers,
                [],
                null,
                GetEquipmentAttackIds(unit, attackerStack, context),
                unit.Name,
                unit.IsTypeLocked);

            builder.Add(newUnit, position);
            index++;
        }

        foreach ((WorldUnit unit, GridPosition position) in defenderUnits.GetUnits())
        {
            ImmutableArray<BattleAbstractStatusEffect>.Builder statusEffects = ImmutableArray.CreateBuilder<BattleAbstractStatusEffect>();

            if (fortificationArmor > 0)
            {
                statusEffects.Add(new BattleFortificationStatusEffect(fortificationArmor));
            }

            BattleUnit newUnit = new(
                new BattleUnitId(index),
                unit.Id,
                unit.DefinitionId,
                BattleSide.Defender,
                unit.Hitpoints,
                unit.Experience,
                0,
                unit.Level,
                unit.Modifiers,
                statusEffects.ToImmutable(),
                null,
                [],
                unit.Name,
                unit.IsTypeLocked);

            builder.Add(newUnit, position);
            index++;
        }

        AddBattleItems(builder, BattleSide.Attacker, attackerStack);

        return builder.Build(modifiers);
    }

    private static ImmutableArray<UnitAttackDefinitionId> GetEquipmentAttackIds(WorldUnit unit, WorldStack stack, IEngineContext context)
    {
        if (context.Definitions.GetUnit(unit.DefinitionId).Leadership is > 0)
        {
            return LeaderEquipmentCalculator.GetEquipmentAttackIds(stack.Equipment, context.Definitions);
        }
        else
        {
            return [];
        }
    }

    private static void AddBattleItems(BattleStateBuilder builder, BattleSide side, WorldStack stack)
    {
        AddBattleItem(builder, side, stack, LeaderEquipmentSlot.Battle1);
        AddBattleItem(builder, side, stack, LeaderEquipmentSlot.Battle2);
    }

    private static void AddBattleItem(BattleStateBuilder builder, BattleSide side, WorldStack stack, LeaderEquipmentSlot slot)
    {
        if (stack.Equipment[slot] is { } item)
        {
            builder.AddItem(new BattleItem(side, slot, item.DefinitionId, item.Charges, Consumed: false));
        }
    }

    public static List<WorldEvent> CreateBattleItemEvents(BattleState finalState, BattleSide side, WorldStack stack)
    {
        List<WorldEvent> events = [];

        foreach (BattleItem item in finalState.Items)
        {
            if (item.Side == side)
            {
                if (CreateBattleItemEvent(stack, item) is { } itemEvent)
                {
                    events.Add(itemEvent);
                }
            }
        }

        return events;
    }

    private static WorldItemUsedEvent? CreateBattleItemEvent(WorldStack stack, BattleItem item)
    {
        WorldItem? equippedItem = stack.Equipment[item.Slot];

        if (equippedItem is null)
        {
            return null;
        }

        if (!item.Consumed && item.Charges == equippedItem.Charges)
        {
            return null;
        }

        return new WorldItemUsedEvent(stack.Id, equippedItem, item.Charges, item.Consumed);
    }

    public static UnitGrid<WorldUnit> BuildSurvivingGrid(
        BattleState finalState,
        BattleSide side,
        WorldStack? duelStack,
        IEngineContext context)
    {
        if (duelStack is not null)
        {
            return BuildDuelSurvivingGrid(finalState, side, duelStack, context);
        }
        else
        {
            return BuildBattleSurvivingGrid(finalState, side, context);
        }
    }

    private static UnitGrid<WorldUnit> BuildBattleSurvivingGrid(
        BattleState finalState,
        BattleSide side,
        IEngineContext context)
    {
        UnitGrid<BattleUnit> baseGrid = finalState.GetGrid(side).GetBaseGrid();
        UnitGrid<WorldUnit>.Builder builder = new();

        foreach ((BattleUnit battleUnit, GridPosition position) in baseGrid.GetUnits())
        {
            if (battleUnit.WorldUnitId is { } worldUnitId)
            {
                WorldUnit worldUnit = new(
                    worldUnitId,
                    battleUnit.DefinitionId,
                    battleUnit.Hitpoints,
                    battleUnit.Experience + battleUnit.ExperienceGained,
                    battleUnit.Level,
                    battleUnit.Modifiers,
                    battleUnit.Name,
                    battleUnit.IsTypeLocked);

                builder.Place(worldUnit, position, context.Definitions.GetUnit(worldUnit.DefinitionId).IsLarge);
            }
        }

        return builder.Build();
    }

    private static UnitGrid<WorldUnit> BuildDuelSurvivingGrid(
        BattleState finalState,
        BattleSide side,
        WorldStack stack,
        IEngineContext context)
    {
        (WorldUnit _, GridPosition leaderPosition, UnitDefinition _) = stack.GetLeader(context.Definitions);
        BattleUnit battleLeader = GetBattleLeader(finalState, side);
        UnitGrid<WorldUnit>.Builder builder = new();

        foreach ((WorldUnit unit, GridPosition position) in stack.Units.GetUnits())
        {
            WorldUnit survivor;

            if (position == leaderPosition)
            {
                survivor = new WorldUnit(
                    unit.Id,
                    battleLeader.DefinitionId,
                    battleLeader.Hitpoints,
                    battleLeader.Experience + battleLeader.ExperienceGained,
                    battleLeader.Level,
                    battleLeader.Modifiers,
                    battleLeader.Name,
                    battleLeader.IsTypeLocked);
            }
            else
            {
                survivor = unit;
            }

            UnitDefinition definition = context.Definitions.GetUnit(survivor.DefinitionId);

            builder.Place(survivor, position, definition.IsLarge);
        }

        return builder.Build();
    }

    private static BattleUnit GetBattleLeader(BattleState finalState, BattleSide side)
    {
        UnitGrid<BattleUnit> baseGrid = finalState.GetGrid(side).GetBaseGrid();

        foreach ((BattleUnit unit, GridPosition _) in baseGrid.GetUnits())
        {
            if (unit.SummonerId is null)
            {
                return unit;
            }
        }

        throw new InvalidOperationException("Stack has no leader.");
    }
}
