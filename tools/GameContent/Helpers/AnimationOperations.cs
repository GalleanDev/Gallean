namespace GameContent;

internal static class AnimationOperations
{
    public static AssetAnimation? TryCreate(IReadOnlyList<AssetSprite>? frames)
    {
        if (frames is { Count: > 0 })
        {
            return Create(frames, triggerFrame: null, secondTriggerFrame: null);
        }
        else
        {
            return null;
        }
    }

    public static AssetAnimation? TryCreate(IReadOnlyList<AssetSprite>? frames, int triggerFrame, int secondTriggerFrame)
    {
        if (frames is { Count: > 0 })
        {
            return Create(frames, triggerFrame, secondTriggerFrame);
        }
        else
        {
            return null;
        }
    }

    public static AssetAnimation Create(IReadOnlyList<AssetSprite> frames, int? triggerFrame, int? secondTriggerFrame)
    {
        int left = int.MaxValue;
        int top = int.MaxValue;
        int right = int.MinValue;
        int bottom = int.MinValue;

        foreach (AssetSprite frame in frames)
        {
            left = Math.Min(left, frame.Metadata.OffsetX);
            top = Math.Min(top, frame.Metadata.OffsetY);
            right = Math.Max(right, frame.Metadata.OffsetX + frame.Metadata.Width);
            bottom = Math.Max(bottom, frame.Metadata.OffsetY + frame.Metadata.Height);
        }

        return new AssetAnimation(
            new AssetAnimationMetadata(frames.Count, right - left, bottom - top, left, top, triggerFrame, secondTriggerFrame),
            frames);
    }
}
