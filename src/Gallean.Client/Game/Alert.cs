namespace Gallean.Client;

internal sealed record Alert(AlertType Type, IconMoniker Icon, string Caption, string? Details);
