using TorchSharp;

namespace Umbra.Ltx;

internal sealed class RefineContext(Dictionary<string, torch.Tensor> contexts, RefinePromptConfiguration promptConfiguration) : IDisposable
{
    public const string FileName = "context.safetensors";

    private static readonly RefinePrompt EmptyPrompt = new(new RefinePromptId("empty"), []);

    public static RefineContext Load(string directory, RefinePromptConfiguration prompts)
    {
        return new RefineContext(SafeTensors.MapStateDict(Path.Combine(directory, FileName)), prompts);
    }

    public RefinePrompt GetPrompt(UmbraSequenceId id)
    {
        return promptConfiguration.Prompts.GetValueOrDefault(id, EmptyPrompt);
    }

    public torch.Tensor GetPromptContext(RefinePromptId prompt)
    {
        return contexts[prompt.Value];
    }

    public void Dispose()
    {
        foreach (torch.Tensor context in contexts.Values)
        {
            context.Dispose();
        }
    }
}
