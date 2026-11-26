namespace Gallean.Client;

internal sealed record WorldActionWheelItemViewModel(
    string Label,
    IconMoniker Icon,
    bool IsHighlighted,
    WorldStackAction Action);
