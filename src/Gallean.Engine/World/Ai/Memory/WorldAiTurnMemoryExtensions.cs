using Gallean.Common;

namespace Gallean.Engine;

internal static class WorldAiTurnMemoryExtensions
{
    extension(WorldAiTurnMemory memory)
    {
        public WorldAiTurnMemory WithAction(IWorldCommand<Empty> command)
        {
            int unitsPurchased = memory.UnitsPurchased;
            int buildingsConstructed = memory.BuildingsConstructed;

            switch (command)
            {
                case WorldPurchaseUnitCommand:
                    unitsPurchased++;
                    break;
                case WorldConstructBuildingCommand:
                    buildingsConstructed++;
                    break;
            }

            return memory with
            {
                ActionsChosen = memory.ActionsChosen + 1,
                UnitsPurchased = unitsPurchased,
                BuildingsConstructed = buildingsConstructed
            };
        }

        public bool IsStackExhausted(WorldStackId stackId)
        {
            return memory.ExhaustedStacks.Contains(stackId);
        }

        public WorldAiTurnMemory WithStackExhausted(WorldStackId stackId)
        {
            return memory with
            {
                ExhaustedStacks = memory.ExhaustedStacks.Add(stackId)
            };
        }
    }
}
