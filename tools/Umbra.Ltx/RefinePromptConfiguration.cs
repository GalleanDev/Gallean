namespace Umbra.Ltx;

public sealed record RefinePromptConfiguration(IReadOnlyDictionary<UmbraSequenceId, RefinePrompt> Prompts);
