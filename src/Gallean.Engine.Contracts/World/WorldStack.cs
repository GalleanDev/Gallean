namespace Gallean.Engine.Contracts;

public sealed record WorldStack(
    WorldStackId Id,
    WorldPlayerId OwnerId,
    WorldPosition Position,
    WorldStackDirection Direction,
    UnitGrid<WorldUnit> Units,
    int MovementPoints,
    WorldInventory Inventory,
    LeaderEquipment Equipment,
    WorldSubraceId SubraceId,
    int AiPriority,
    bool AiIgnore,
    bool Concealed,
    WorldCanonicalStackOrder Order,
    int? ExpirationTurn,
    int PendingLeaderUpgrades,
    int BattlesWon,
    ScenarioStackTemplateId? SourceTemplateId,
    bool LeaderUpgradePostponed = false);
