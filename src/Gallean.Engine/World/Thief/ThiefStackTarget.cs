namespace Gallean.Engine;

internal sealed record ThiefStackTarget(WorldStack Stack, int Protection)
    : ThiefTarget(Stack.OwnerId, Protection);
