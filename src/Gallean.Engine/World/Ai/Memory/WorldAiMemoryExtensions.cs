namespace Gallean.Engine;

internal static class WorldAiMemoryExtensions
{
    extension(WorldAiMemory memory)
    {
        public WorldAiMemory ReconcileWith(WorldState state)
        {
            WorldTurnInfo turnInfo = state.TurnInfo;

            if (memory.Turn.TurnNumber != turnInfo.TurnNumber || memory.Turn.PlayerId != turnInfo.CurrentPlayerId)
            {
                return memory.WithTurnStarted(turnInfo);
            }

            return memory;
        }

        public WorldAiStackMemory GetStackMemory(WorldStack stack)
        {
            return memory.Stacks.GetValueOrDefault(stack.Id, WorldAiStackMemory.Empty);
        }

        public WorldAiMemory WithStackMemory(WorldStackId stackId, WorldAiStackMemory stackMemory)
        {
            return memory with
            {
                Stacks = memory.Stacks.SetItem(stackId, stackMemory)
            };
        }

        private WorldAiMemory WithTurnStarted(WorldTurnInfo turnInfo)
        {
            return memory with
            {
                Turn = new WorldAiTurnMemory(turnInfo.TurnNumber, turnInfo.CurrentPlayerId)
            };
        }
    }
}
