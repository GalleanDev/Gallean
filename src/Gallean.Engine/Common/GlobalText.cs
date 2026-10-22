namespace Gallean.Engine;

public sealed record GlobalText(
    GlobalTextId Id,
    string Text,
    bool? IsVerified,
    string? Context);
