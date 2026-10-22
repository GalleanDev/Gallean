namespace Umbra;

public sealed record UmbraSequenceGroup(UmbraSequenceId Id, IReadOnlyList<UmbraSequence> Sequences);
