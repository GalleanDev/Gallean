namespace Gallean.Engine;

public sealed record WorldPathSegment(
    WorldPosition Start,
    WorldPosition End,
    int Cost,
    int Turn);
