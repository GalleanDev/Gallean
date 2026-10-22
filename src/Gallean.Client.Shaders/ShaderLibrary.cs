using SkiaSharp;

namespace Gallean.Client.Shaders;

internal static class ShaderLibrary
{
    public static SKRuntimeEffect Compile(ShaderKey key)
    {
        SKRuntimeEffect? effect = SKRuntimeEffect.CreateShader(ReadSource(key), out string errors);

        if (effect is null)
        {
            throw new InvalidOperationException($"Failed to compile shader '{key.Name}': {errors}.");
        }

        return effect;
    }

    private static string ReadSource(ShaderKey key)
    {
        Stream? stream = typeof(ShaderLibrary).Assembly.GetManifestResourceStream(key.Resource);

        if (stream is null)
        {
            throw new KeyNotFoundException($"No shader resource named '{key.Resource}'.");
        }

        using StreamReader reader = new(stream);

        return reader.ReadToEnd();
    }
}
