using System.Text;
using TorchSharp;

namespace Umbra.Tests;

internal static class TestHelpers
{
    public static UmbraSpriteHash MakeSpriteHash(string text)
    {
        return new UmbraSpriteHash(Encoding.UTF8.GetBytes(text));
    }

    public static string FormatSpriteHash(UmbraSpriteHash hash)
    {
        return Encoding.UTF8.GetString(hash.Value.Span);
    }

    public static float MaxDifference(torch.Tensor actual, torch.Tensor expected)
    {
        return (actual - expected).abs().max().ToSingle();
    }
}
