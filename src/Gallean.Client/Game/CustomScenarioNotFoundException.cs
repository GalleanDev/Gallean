namespace Gallean.Client;

internal sealed class CustomScenarioNotFoundException(string id)
    : Exception($"Custom scenario '{id}' was not found.");
