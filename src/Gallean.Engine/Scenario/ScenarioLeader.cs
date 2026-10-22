namespace Gallean.Engine;

public sealed record ScenarioLeader(
    WorldUnit Unit,
    LeaderEquipment Equipment,
    WorldInventory Inventory,
    int PendingLeaderUpgrades,
    int BattlesWon);
