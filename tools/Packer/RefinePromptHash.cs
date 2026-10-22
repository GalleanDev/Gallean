namespace Packer;

internal static class RefinePromptHash
{
    private const string EmptyPromptId = "empty";

    public static UmbraPromptHash Get(AssetKey key)
    {
        RefinePrompt? prompt = RefinePromptMap.GetPrompt(key);
        string id = prompt?.Id.Value ?? EmptyPromptId;

        return Get(id);
    }

    public static UmbraPromptHash Get(string id)
    {
        return PromptMap.GetHash(id);
    }
}
