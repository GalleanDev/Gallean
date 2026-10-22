namespace Umbra.Backgrounds;

public sealed record UmbraBackgroundImage(
    string Name,
    UmbraBackgroundLayer Color,
    UmbraBackgroundLayer Normal,
    UmbraBackgroundLayer Depth,
    UmbraBackgroundLayer Albedo,
    IReadOnlyList<UmbraBackgroundLayer> Fluids);
