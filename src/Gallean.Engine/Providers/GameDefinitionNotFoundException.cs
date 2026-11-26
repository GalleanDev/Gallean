namespace Gallean.Engine;

public sealed class GameDefinitionNotFoundException(string id, string message)
    : Exception(message)
{
    public string Id { get; } = id;
}
