namespace Gallean.Engine;

public static class WorldStackExtensions
{
    extension(WorldStack stack)
    {
        public (WorldUnit Unit, GridPosition Position, UnitDefinition Definition) GetLeader(IGameDefinitionProvider definitions)
        {
            foreach ((WorldUnit unit, GridPosition position) in stack.Units.GetUnits())
            {
                UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

                if (definition.Leadership is > 0)
                {
                    return (unit, position, definition);
                }
            }

            throw new InvalidOperationException($"Stack '{stack.Id}' has no leader.");
        }

        public LeaderEquipmentSlot? GetEquipmentSlot(WorldItemId itemId)
        {
            foreach ((LeaderEquipmentSlot slot, WorldItem item) in stack.Equipment.Items)
            {
                if (item.Id == itemId)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
