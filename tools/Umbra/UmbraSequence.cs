namespace Umbra;

public sealed record UmbraSequence(UmbraSequenceId Id, IReadOnlyList<UmbraFrame> Frames);
