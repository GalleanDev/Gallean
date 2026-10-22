using Avalonia;
using Avalonia.Headless.NUnit;
using SkiaSharp;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class CursorAnimationTests
{
    [AvaloniaTest]
    public void HotSpotInsideTest()
    {
        TestAssetProviderBuilder builder = new();
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(2, 61, 59, -31, -31, TriggerFrame: null, SecondTriggerFrame: null),
            [
                builder.CreateSprite(SKColors.Red, 61, 59, -31, -31),
                builder.CreateSprite(SKColors.Blue, 40, 40, -20, -20)
            ]);

        using CursorAnimation cursorAnimation = CursorAnimation.Create(animation, overlay: null, dragImage: null, 1.0);

        Assert.That(cursorAnimation.FrameCount, Is.EqualTo(2));
        Assert.That(cursorAnimation.Size, Is.EqualTo(new PixelSize(61, 59)));
        Assert.That(cursorAnimation.HotSpot, Is.EqualTo(new PixelPoint(31, 31)));
    }

    [AvaloniaTest]
    public void HotSpotOutsideTest()
    {
        TestAssetProviderBuilder builder = new();
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(1, 39, 46, 4, 6, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Red, 39, 46, 4, 6)]);

        using CursorAnimation cursorAnimation = CursorAnimation.Create(animation, overlay: null, dragImage: null, 1.0);

        Assert.That(cursorAnimation.FrameCount, Is.EqualTo(1));
        Assert.That(cursorAnimation.Size, Is.EqualTo(new PixelSize(43, 52)));
        Assert.That(cursorAnimation.HotSpot, Is.EqualTo(new PixelPoint(0, 0)));
    }

    [AvaloniaTest]
    public void HotSpotBeyondTest()
    {
        TestAssetProviderBuilder builder = new();
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(1, 29, 19, -28, -19, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Red, 29, 19, -28, -19)]);

        using CursorAnimation cursorAnimation = CursorAnimation.Create(animation, overlay: null, dragImage: null, 1.0);

        Assert.That(cursorAnimation.FrameCount, Is.EqualTo(1));
        Assert.That(cursorAnimation.Size, Is.EqualTo(new PixelSize(29, 20)));
        Assert.That(cursorAnimation.HotSpot, Is.EqualTo(new PixelPoint(28, 19)));
    }

    [AvaloniaTest]
    public void OverlayTest()
    {
        TestAssetProviderBuilder builder = new();
        Animation arrow = builder.CreateAnimation(
            new AssetAnimationMetadata(1, 24, 54, -3, -3, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Red, 24, 54, -3, -3)]);
        Animation overlay = builder.CreateAnimation(
            new AssetAnimationMetadata(2, 40, 30, 20, -10, TriggerFrame: null, SecondTriggerFrame: null),
            [
                builder.CreateSprite(SKColors.Blue, 40, 30, 20, -10),
                builder.CreateSprite(SKColors.Green, 30, 30, 30, -10)
            ]);

        using CursorAnimation cursorAnimation = CursorAnimation.Create(arrow, overlay, dragImage: null, 1.0);

        Assert.That(cursorAnimation.FrameCount, Is.EqualTo(2));
        Assert.That(cursorAnimation.Size, Is.EqualTo(new PixelSize(63, 61)));
        Assert.That(cursorAnimation.HotSpot, Is.EqualTo(new PixelPoint(3, 10)));
    }

    [AvaloniaTest]
    public void ScalingTest()
    {
        TestAssetProviderBuilder builder = new();
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(1, 24, 54, 0, 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Red, 24, 54, 0, 0)]);

        using CursorAnimation cursorAnimation = CursorAnimation.Create(animation, overlay: null, dragImage: null, 1.5);

        Assert.That(cursorAnimation.FrameCount, Is.EqualTo(1));
        Assert.That(cursorAnimation.Size, Is.EqualTo(new PixelSize(36, 81)));
        Assert.That(cursorAnimation.HotSpot, Is.EqualTo(new PixelPoint(0, 0)));
    }

    [AvaloniaTest]
    public void DragImageTest()
    {
        TestAssetProviderBuilder builder = new();
        Animation arrow = builder.CreateAnimation(
            new AssetAnimationMetadata(1, 24, 54, -3, -3, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Red, 24, 54, -3, -3)]);

        using SKSurface surface = SKSurface.Create(new SKImageInfo(76, 91));
        surface.Canvas.Clear(SKColors.Blue);
        using SKImage image = surface.Snapshot();

        using CursorAnimation cursorAnimation = CursorAnimation.Create(arrow, overlay: null, new CursorDragImage(image, new Size(76.0, 91.0)), 1.0);

        Assert.That(cursorAnimation.FrameCount, Is.EqualTo(1));
        Assert.That(cursorAnimation.Size, Is.EqualTo(new PixelSize(76, 97)));
        Assert.That(cursorAnimation.HotSpot, Is.EqualTo(new PixelPoint(38, 46)));
    }
}
