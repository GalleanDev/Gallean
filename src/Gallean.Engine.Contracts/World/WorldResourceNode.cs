namespace Gallean.Engine.Contracts;

public sealed record WorldResourceNode(
    WorldResourceNodeId Id,
    WorldPlayerId OwnerId,
    WorldPosition Position,
    WorldResourceType ResourceType,
    int AiPriority);
