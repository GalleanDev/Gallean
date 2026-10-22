namespace Packer.Tests;

internal sealed record TestHttpResource(
    byte[] Contents,
    IReadOnlyDictionary<string, string> Headers,
    string? RedirectsTo);
