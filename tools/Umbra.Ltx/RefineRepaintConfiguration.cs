namespace Umbra.Ltx;

public sealed record RefineRepaintConfiguration(IReadOnlyDictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>> Regions);
