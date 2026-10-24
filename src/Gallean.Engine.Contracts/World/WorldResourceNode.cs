namespace Gallean.Engine.Contracts;

public sealed record WorldResourceNode(
    WorldResourceNodeId Id,
    WorldPosition Position,
    WorldResourceType ResourceType,
    int AiPriority);
