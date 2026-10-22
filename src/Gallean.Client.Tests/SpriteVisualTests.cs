using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Gallean.Common;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class SpriteVisualTests
{
    [AvaloniaTest]
    public void MissingAssetScopeTest()
    {
        SpriteVisual sprite = new();

        Assert.DoesNotThrow(() => sprite.SpriteKey = new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0));
    }

    [AvaloniaTest]
    public void RemovedFromAssetScopeTest()
    {
        SpriteVisual sprite = new()
        {
            SpriteKey = new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0)
        };

        Panel panel = new();
        AssetScope.SetAssets(panel, NullAssetProvider.Instance);
        panel.Children.Add(sprite);

        Assert.DoesNotThrow(() => panel.Children.Clear());
    }
}
