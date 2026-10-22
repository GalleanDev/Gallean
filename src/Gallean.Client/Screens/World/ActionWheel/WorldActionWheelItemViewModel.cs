namespace Gallean.Client;

internal sealed record WorldActionWheelItemViewModel(
    string Label,
    IconMoniker? Icon,
    AssetSpriteKey.ItemIcon? ItemIconKey,
    bool IsHighlighted,
    WorldStackAction Action);
